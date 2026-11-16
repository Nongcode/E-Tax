using PuppeteerSharp;

namespace E_Tax
{
    /// <summary>
    /// Convert HTML hóa đơn sang PDF bằng Chromium (PuppeteerSharp).
    /// Trình duyệt chỉ được mở ở lần convert đầu tiên và dùng chung cho cả lô; đóng khi Dispose.
    /// </summary>
    internal sealed class PdfConverter : IAsyncDisposable
    {
        private readonly Func<Task<string>> _getChromiumPath;
        private IBrowser? _browser;

        public PdfConverter(Func<Task<string>> getChromiumPath)
        {
            _getChromiumPath = getChromiumPath;
        }

        public async Task ConvertAsync(string htmlPath, string pdfPath)
        {
            // Mở lại nếu trình duyệt đã bị đóng/crash giữa chừng
            if (_browser == null || !_browser.IsConnected)
            {
                if (_browser != null) await _browser.DisposeAsync();
                string chromiumPath = await _getChromiumPath();
                _browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, ExecutablePath = chromiumPath });
            }

            await using var page = await _browser.NewPageAsync();
            await page.GoToAsync("file:///" + htmlPath.Replace('\\', '/'),
                new NavigationOptions { Timeout = 60000, WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } });
            await page.PdfAsync(pdfPath, new PdfOptions { Format = PuppeteerSharp.Media.PaperFormat.A4 });
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser != null)
            {
                await _browser.DisposeAsync();
                _browser = null;
            }
        }
    }
}
