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
}
