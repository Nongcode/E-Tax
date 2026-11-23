using System.Net;

namespace E_Tax.Tests
{
    public class RiskCheckServiceTests
    {
        private const string Secret = "SECRET-TOKEN-123";

        private static RiskCheckService Make(FakeHttpHandler handler, List<string>? logs = null) =>
            new RiskCheckService(new HttpClient(handler), Secret, logs == null ? (_ => { }) : (m => { lock (logs) logs.Add(m); }));

        private static RiskCheckService Service(string body, HttpStatusCode status = HttpStatusCode.OK) =>
            Make(new FakeHttpHandler((_, _) => FakeHttpHandler.Json(body, status)));

        [Theory]
        [InlineData("{\"data\":{\"name\":\"X\",\"blacklisted\":true}}", "Risky")]
        [InlineData("{\"blacklisted\":\"1\"}", "Risky")]
        [InlineData("{\"blacklisted\":1}", "Risky")]
        [InlineData("{\"blacklisted\":\"TRUE\"}", "Risky")]
        [InlineData("{\"blacklisted\":0}", "Safe")]
        [InlineData("{\"blacklisted\":false}", "Safe")]
        [InlineData("{\"blacklisted\":\"false\"}", "Safe")]
        [InlineData("{\"blacklisted\":\"0\"}", "Safe")]
        [InlineData("{\"blacklisted\":\"maybe\"}", "Unknown")]
        [InlineData("{\"blacklisted\":2}", "Unknown")]
        [InlineData("{\"blacklisted\":null}", "Unknown")]
        [InlineData("{\"blacklisted\":[]}", "Unknown")]
        [InlineData("{\"data\":{\"name\":\"X\"}}", "Unknown")]
        [InlineData("{}", "Unknown")]
        [InlineData("[]", "Unknown")]
        [InlineData("{\"success\":false,\"message\":\"x\"}", "Unknown")]
        [InlineData("{\"success\":false,\"blacklisted\":false}", "Unknown")]
        [InlineData("{\"error\":\"token sai\",\"blacklisted\":false}", "Unknown")]
        [InlineData("không phải json", "Unknown")]
        [InlineData("", "Unknown")]
        public async Task Check_MapsResponse(string body, string expected) =>
            Assert.Equal(expected, (await Service(body).CheckAsync("0101234567")).Status.ToString());

        [Fact]
        public async Task Check_ReadsCompanyName()
        {
            var r = await Service("{\"data\":{\"name\":\"Công ty X\",\"blacklisted\":false}}").CheckAsync(" 0101234567 ");
            Assert.Equal("0101234567", r.TaxCode);
            Assert.Equal("Công ty X", r.CompanyName);
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound)]
        public async Task Check_NonSuccessHttp_IsUnknown_EvenIfBodyLooksSafe(HttpStatusCode status) =>
            Assert.Equal(RiskStatus.Unknown, (await Service("{\"blacklisted\":false}", status).CheckAsync("0101234567")).Status);

        [Fact]
        public async Task Check_NetworkError_IsUnknown()
        {
            var svc = Make(new FakeHttpHandler((_, _) => throw new HttpRequestException("mất mạng")));
            Assert.Equal(RiskStatus.Unknown, (await svc.CheckAsync("0101234567")).Status);
        }

        [Fact]
        public async Task Check_Timeout_NotCancelledByCaller_IsUnknown()
        {
            var svc = Make(new FakeHttpHandler((_, _) => throw new TaskCanceledException("timeout")));
            Assert.Equal(RiskStatus.Unknown, (await svc.CheckAsync("0101234567")).Status);
        }

        [Fact]
        public async Task Check_CancelledByCaller_Rethrows()
        {
            var svc = Make(new FakeHttpHandler((_, _) => FakeHttpHandler.Json("{\"blacklisted\":false}"), TimeSpan.FromSeconds(5)));
            using var cts = new CancellationTokenSource(50);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => svc.CheckAsync("0101234567", cts.Token));
        }

        [Fact]
        public async Task CheckMany_Cancelled_Throws()
        {
            var handler = new FakeHttpHandler((_, _) => FakeHttpHandler.Json("{\"blacklisted\":false}"), TimeSpan.FromSeconds(5));
            using var cts = new CancellationTokenSource(50);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                Make(handler).CheckManyAsync(new[] { "1", "2", "3", "4", "5" }, null, cts.Token));
        }

        [Fact]
        public async Task CheckMany_DedupesAndLimitsConcurrency()
        {
            var handler = new FakeHttpHandler((_, _) => FakeHttpHandler.Json("{\"blacklisted\":false}"), TimeSpan.FromMilliseconds(50));
            int completed = 0;
            var r = await Make(handler).CheckManyAsync(new[] { "1", "1", " 1 ", "2", "3", "4", "5", " ", "" }, _ => Interlocked.Increment(ref completed));
            Assert.Equal(5, r.Count);
            Assert.Equal(5, handler.RequestCount);
            Assert.Equal(5, completed);
            Assert.True(handler.MaxConcurrent <= RiskCheckService.MaxConcurrency);
            Assert.Equal(3, RiskCheckService.MaxConcurrency);
        }

        [Fact]
        public async Task Check_NeverLogsToken()
        {
            var logs = new List<string>();
            var failing = Make(new FakeHttpHandler((_, _) => throw new HttpRequestException("lỗi tại https://x/?token=" + Secret)), logs);
            await failing.CheckAsync("0101234567");
            var bad = Make(new FakeHttpHandler((_, _) => FakeHttpHandler.Json("{\"blacklisted\":false,\"echo\":\"" + Secret + "\"}", HttpStatusCode.InternalServerError)), logs);
            await bad.CheckAsync("0101234567");
            var junk = Make(new FakeHttpHandler((_, _) => FakeHttpHandler.Json("rác " + Secret)), logs);
            await junk.CheckAsync("0101234567");

            Assert.NotEmpty(logs);
            string all = string.Join("\n", logs);
            Assert.DoesNotContain(Secret, all);
            Assert.DoesNotContain("token=", all);
            Assert.DoesNotContain("metaecom", all);
            Assert.DoesNotContain("rác", all);
        }

        [Fact]
        public async Task Check_SendsTokenInUrlOnly()
        {
            var handler = new FakeHttpHandler((_, _) => FakeHttpHandler.Json("{\"blacklisted\":false}"));
            await Make(handler).CheckAsync("0101234567");
            Assert.Single(handler.Urls);
            Assert.Contains("tax_code=0101234567", handler.Urls[0]);
            Assert.Contains(Secret, handler.Urls[0]);
        }

        [Fact]
        public async Task CheckMany_OneFailure_OthersStillReturned()
        {
            var handler = new FakeHttpHandler((req, _) =>
                req.RequestUri!.Query.Contains("tax_code=bad")
                    ? throw new HttpRequestException("x")
                    : FakeHttpHandler.Json("{\"blacklisted\":true}"));
            var r = await Make(handler).CheckManyAsync(new[] { "ok", "bad" });
            Assert.Equal(RiskStatus.Unknown, r["bad"].Status);
            Assert.Equal(RiskStatus.Risky, r["ok"].Status);
        }

        [Theory]
        [InlineData("Risky", "Rủi ro")]
        [InlineData("Safe", "An toàn")]
        [InlineData("Unknown", "Không kiểm tra được")]
        public void Label_UsesVietnameseLabels(string status, string expected) =>
            Assert.Equal(expected, RiskCheckService.Label(Enum.Parse<RiskStatus>(status)));
    }
}
