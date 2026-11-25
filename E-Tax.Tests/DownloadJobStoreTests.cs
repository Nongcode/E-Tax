using E_Tax;

namespace E_Tax.Tests
{
    public class DownloadJobStoreTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "etax-jobs-" + Guid.NewGuid());

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private DownloadJobStore NewStore() => new(_dir);

        private static string[] Keys(int n) => Enumerable.Range(0, n).Select(i => "k" + i).ToArray();

        [Fact]
        public void MarkAndFlush_SurviveNewInstance_With10000Keys()
        {
            var store = NewStore();
            string id = store.Create("T", Keys(10000));
            store.Mark(id, "k0", DownloadItemState.Done, null);
            store.Mark(id, "k1", DownloadItemState.Failed, "loi");
            store.Flush(id);

            var reopened = NewStore();
            var summary = Assert.Single(reopened.List());
            Assert.Equal(9998, summary.Pending);
            Assert.Equal(1, summary.Done);
            Assert.Equal(1, summary.Failed);
            Assert.DoesNotContain("k0", reopened.PendingKeys(id));
            Assert.DoesNotContain("k1", reopened.PendingKeys(id));
        }

        [Fact]
        public void SingleInstance_ConcurrentMarkListFlush_DoNotThrow()
        {
            var store = NewStore();
            string id = store.Create("T", Keys(2000));
            var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var writer = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 2000; i++)
                    {
                        store.Mark(id, "k" + i, i % 2 == 0 ? DownloadItemState.Done : DownloadItemState.Failed, "e");
                        if (i % 200 == 0) store.Flush(id);
                    }
                }
                catch (Exception ex) { errors.Add(ex); }
            });
            var reader = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 300; i++) { store.List(); store.PendingKeys(id); store.RetryFailed(id); }
                }
                catch (Exception ex) { errors.Add(ex); }
            });
            Task.WaitAll(writer, reader);
            Assert.Empty(errors);
        }

        [Fact]
        public void RetryFailed_MovesFailedToPending()
        {
            var store = NewStore();
            string id = store.Create("T", Keys(3));
            store.Mark(id, "k0", DownloadItemState.Failed, "x");
            store.Mark(id, "k1", DownloadItemState.Done, null);

            Assert.Equal(1, store.RetryFailed(id));
            Assert.Contains("k0", store.PendingKeys(id));
            Assert.Equal(0, store.RetryFailed(id));
        }

        [Fact]
        public void List_IgnoresCorruptJobFile()
        {
            var store = NewStore();
            string good = store.Create("T", Keys(2));
            File.WriteAllText(Path.Combine(_dir, new string('a', 32) + ".json"), "{ not json");

            var list = NewStore().List();
            Assert.Equal(good, Assert.Single(list).JobId);
        }

        [Fact]
        public void Error_IsRedactedAndTruncated()
        {
            var store = NewStore();
            string id = store.Create("T", Keys(1));
            store.Mark(id, "k0", DownloadItemState.Failed,
                "GET https://x/y?a=1&token=abc123&b=2 failed " + new string('z', 500));
            store.Flush(id);

            string raw = File.ReadAllText(Directory.GetFiles(_dir, "*.json").Single());
            Assert.DoesNotContain("abc123", raw);
            Assert.Contains("token=***", raw);
            Assert.DoesNotContain(new string('z', 201), raw);
        }

        [Fact]
        public void DuplicateKeys_CountedOnce()
        {
            var store = NewStore();
            string id = store.Create("T", new[] { "a", "a", "b" });
            Assert.Equal(2, store.PendingKeys(id).Count);
        }

        [Fact]
        public void FindResumable_SameTitleAndKeysWithPending_ReturnsJob()
        {
            var store = NewStore();
            string id = store.Create("C:\\out", Keys(3));
            store.Mark(id, "k0", DownloadItemState.Done, null);
            store.Flush(id);

            Assert.Equal(id, NewStore().FindResumable("C:\\out", Keys(3).Reverse()));
        }

        [Fact]
        public void FindResumable_FailedItemsCountAsResumable()
        {
            var store = NewStore();
            string id = store.Create("T", Keys(2));
            store.Mark(id, "k0", DownloadItemState.Done, null);
            store.Mark(id, "k1", DownloadItemState.Failed, "x");
            store.Flush(id);

            Assert.Equal(id, store.FindResumable("T", Keys(2)));
        }

        [Fact]
        public void FindResumable_DifferentKeysOrTitle_ReturnsNull()
        {
            var store = NewStore();
            store.Create("T", Keys(3));

            Assert.Null(store.FindResumable("T", Keys(2)));
            Assert.Null(store.FindResumable("T", Keys(4)));
            Assert.Null(store.FindResumable("T", new[] { "k0", "k1", "other" }));
            Assert.Null(store.FindResumable("U", Keys(3)));
        }

        [Fact]
        public void FindResumable_FullyDoneJob_ReturnsNullButStaysListed()
        {
            var store = NewStore();
            string id = store.Create("T", Keys(2));
            store.Mark(id, "k0", DownloadItemState.Done, null);
            store.Mark(id, "k1", DownloadItemState.Done, null);
            store.Flush(id);

            Assert.Null(store.FindResumable("T", Keys(2)));
            Assert.Single(store.List());
        }

        [Fact]
        public void FindResumable_NewestOfTwoMatchesWins()
        {
            var store = NewStore();
            string older = store.Create("T", Keys(2));
            Thread.Sleep(30);
            string newer = store.Create("T", Keys(2));

            Assert.Equal(newer, NewStore().FindResumable("T", Keys(2)));
            Assert.NotEqual(older, newer);
        }
    }

    public class DownloadJobStoreExtraTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "etax-jobs2-" + Guid.NewGuid());
        private readonly string _work = Path.Combine(Path.GetTempPath(), "etax-workroot-" + Guid.NewGuid());

        public DownloadJobStoreExtraTests() => Directory.CreateDirectory(_work);

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            if (Directory.Exists(_work)) Directory.Delete(_work, true);
        }

        private static string[] Keys(int n) => Enumerable.Range(0, n).Select(i => "k" + i).ToArray();

        [Fact]
        public void FindLatest_ReturnsFullyDoneJob()
        {
            var store = new DownloadJobStore(_dir);
            string id = store.Create("T", Keys(2));
            store.Mark(id, "k0", DownloadItemState.Done, null);
            store.Mark(id, "k1", DownloadItemState.Done, null);
            store.Flush(id);

            Assert.Equal(id, new DownloadJobStore(_dir).FindLatest("T", Keys(2)));
            Assert.Null(store.FindResumable("T", Keys(2)));
        }

        [Fact]
        public void FindLatest_DifferentKeys_ReturnsNull()
        {
            var store = new DownloadJobStore(_dir);
            store.Create("T", Keys(2));
            Assert.Null(store.FindLatest("T", Keys(3)));
            Assert.Null(store.FindLatest("U", Keys(2)));
        }

        [Fact]
        public void DeleteOrphans_RemovesJobWhoseFolderIsGone()
        {
            var store = new DownloadJobStore(_dir);
            string gone = store.Create(Path.Combine(_work, new string('a', 64)), Keys(1));

            Assert.Equal(1, store.DeleteOrphans(_work));
            Assert.Empty(store.List());
            Assert.Empty(Directory.GetFiles(_dir, gone + "*"));
        }

        [Fact]
        public void DeleteOrphans_KeepsJobWhoseFolderExists()
        {
            var store = new DownloadJobStore(_dir);
            string live = Path.Combine(_work, new string('b', 64));
            Directory.CreateDirectory(live);
            store.Create(live, Keys(1));

            Assert.Equal(0, store.DeleteOrphans(_work));
            Assert.Single(store.List());
        }

        [Fact]
        public void DeleteOrphans_KeepsJobWithTitleOutsideRoot()
        {
            var store = new DownloadJobStore(_dir);
            store.Create(Path.Combine(Path.GetTempPath(), "etax-elsewhere-" + Guid.NewGuid()), Keys(1));
            store.Create(_work + "-sibling", Keys(1));
            store.Create("not a path", Keys(1));

            Assert.Equal(0, store.DeleteOrphans(_work));
            Assert.Equal(3, store.List().Count);
        }
    }

    public class DownloadWorkFolderTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "etax-work-" + Guid.NewGuid());

        public DownloadWorkFolderTests() => Directory.CreateDirectory(_root);

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [Fact]
        public void PathFor_SameKeysAnyOrder_SamePath()
        {
            string a = DownloadWorkFolder.PathFor(_root, new[] { "b", "a", "c", "a" });
            string b = DownloadWorkFolder.PathFor(_root, new[] { "c", "a", "b" });
            Assert.Equal(a, b);
            Assert.Equal(_root, Path.GetDirectoryName(a));
            Assert.Matches("^[0-9a-f]{64}$", Path.GetFileName(a));
        }

        [Fact]
        public void PathFor_DifferentKeys_DifferentPath()
        {
            Assert.NotEqual(
                DownloadWorkFolder.PathFor(_root, new[] { "a", "b" }),
                DownloadWorkFolder.PathFor(_root, new[] { "a", "c" }));
        }

        [Fact]
        public void CleanupStale_RemovesOnlyOldHashNamedFolders()
        {
            var now = DateTime.UtcNow;
            string oldHash = Path.Combine(_root, new string('a', 64));
            string newHash = Path.Combine(_root, new string('b', 64));
            string oldOther = Path.Combine(_root, "keep-me");
            string oldUpper = Path.Combine(_root, new string('D', 64));
            foreach (var d in new[] { oldHash, newHash, oldOther, oldUpper }) Directory.CreateDirectory(d);
            string oldFile = Path.Combine(_root, new string('c', 64));
            File.WriteAllText(oldFile, "x");
            foreach (var d in new[] { oldHash, oldOther, oldUpper }) Directory.SetLastWriteTimeUtc(d, now.AddDays(-40));
            File.SetLastWriteTimeUtc(oldFile, now.AddDays(-40));

            int removed = DownloadWorkFolder.CleanupStale(_root, now, TimeSpan.FromDays(30));

            Assert.Equal(1, removed);
            Assert.False(Directory.Exists(oldHash));
            Assert.True(Directory.Exists(newHash));
            Assert.True(Directory.Exists(oldOther));
            Assert.True(Directory.Exists(oldUpper));
            Assert.True(File.Exists(oldFile));
        }

        [Fact]
        public void CanSkip_DoneWithAllFilesNonEmpty_True()
        {
            string f1 = Path.Combine(_root, "1.zip"), f2 = Path.Combine(_root, "1.pdf");
            File.WriteAllText(f1, "x");
            File.WriteAllText(f2, "x");
            Assert.True(DownloadWorkFolder.CanSkip(DownloadItemState.Done, new[] { f1, f2 }));
        }

        [Fact]
        public void CanSkip_FileMissing_False()
        {
            string f1 = Path.Combine(_root, "1.zip");
            File.WriteAllText(f1, "x");
            Assert.False(DownloadWorkFolder.CanSkip(DownloadItemState.Done, new[] { f1, Path.Combine(_root, "none.pdf") }));
        }

        [Fact]
        public void CanSkip_EmptyFile_False()
        {
            string f1 = Path.Combine(_root, "1.pdf");
            File.WriteAllBytes(f1, Array.Empty<byte>());
            Assert.False(DownloadWorkFolder.CanSkip(DownloadItemState.Done, new[] { f1 }));
        }

        [Fact]
        public void CanSkip_NotDone_False()
        {
            string f1 = Path.Combine(_root, "1.pdf");
            File.WriteAllText(f1, "x");
            Assert.False(DownloadWorkFolder.CanSkip(DownloadItemState.Failed, new[] { f1 }));
            Assert.False(DownloadWorkFolder.CanSkip(DownloadItemState.Pending, new[] { f1 }));
        }

        private string MakeZip(string name, params string[] entries)
        {
            string path = Path.Combine(_root, name);
            using var zip = System.IO.Compression.ZipFile.Open(path, System.IO.Compression.ZipArchiveMode.Create);
            foreach (string e in entries)
            {
                using var w = new StreamWriter(zip.CreateEntry(e).Open());
                w.Write("x");
            }
            return path;
        }

        [Fact]
        public void CanSkipInvoice_ZipWithXmlAndXmlPresent_True()
        {
            string zip = MakeZip("a.zip", "a.pdf", "a.xml");
            string pdf = Path.Combine(_root, "a.pdf"), xml = Path.Combine(_root, "a.xml");
            File.WriteAllText(pdf, "x");
            File.WriteAllText(xml, "x");
            Assert.True(DownloadWorkFolder.CanSkipInvoice(zip, pdf, xml));
        }

        [Fact]
        public void CanSkipInvoice_ZipWithXmlAndXmlMissing_False()
        {
            string zip = MakeZip("b.zip", "b.pdf", "b.XML");
            string pdf = Path.Combine(_root, "b.pdf");
            File.WriteAllText(pdf, "x");
            Assert.False(DownloadWorkFolder.CanSkipInvoice(zip, pdf, Path.Combine(_root, "b.xml")));
        }

        [Fact]
        public void CanSkipInvoice_ZipWithoutXml_True()
        {
            string zip = MakeZip("c.zip", "c.pdf");
            string pdf = Path.Combine(_root, "c.pdf");
            File.WriteAllText(pdf, "x");
            Assert.True(DownloadWorkFolder.CanSkipInvoice(zip, pdf, Path.Combine(_root, "c.xml")));
        }

        [Fact]
        public void CanSkipInvoice_CorruptZip_False()
        {
            string zip = Path.Combine(_root, "d.zip");
            File.WriteAllText(zip, "not a zip");
            string pdf = Path.Combine(_root, "d.pdf");
            File.WriteAllText(pdf, "x");
            Assert.False(DownloadWorkFolder.CanSkipInvoice(zip, pdf, Path.Combine(_root, "d.xml")));
        }

        [Fact]
        public void CanSkipInvoice_PdfMissing_False()
        {
            string zip = MakeZip("e.zip", "e.pdf");
            Assert.False(DownloadWorkFolder.CanSkipInvoice(zip, Path.Combine(_root, "e.pdf"), Path.Combine(_root, "e.xml")));
        }

        [Fact]
        public void RemoveMergedOutputs_DeletesMergedAndItsTempFilesOnly()
        {
            File.WriteAllText(Path.Combine(_root, "_GopTatCa.pdf"), "old");
            File.WriteAllText(Path.Combine(_root, "._GopTatCa.pdf.abc123.tmp"), "t");
            File.WriteAllText(Path.Combine(_root, "HD_1.pdf"), "keep");
            File.WriteAllText(Path.Combine(_root, "other.tmp"), "keep");

            DownloadWorkFolder.RemoveMergedOutputs(_root, "_GopTatCa.pdf");

            Assert.False(File.Exists(Path.Combine(_root, "_GopTatCa.pdf")));
            Assert.False(File.Exists(Path.Combine(_root, "._GopTatCa.pdf.abc123.tmp")));
            Assert.True(File.Exists(Path.Combine(_root, "HD_1.pdf")));
            Assert.True(File.Exists(Path.Combine(_root, "other.tmp")));
        }

        [Fact]
        public void RemoveMergedOutputs_MissingFolder_DoesNothing()
        {
            DownloadWorkFolder.RemoveMergedOutputs(Path.Combine(_root, "nope"), "_GopTatCa.pdf");
        }
    }
}
