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
    }
}
