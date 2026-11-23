using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace E_Tax
{
    internal sealed record PdfMergeResult(int PagesWritten, IReadOnlyList<string> FailedFiles);

    /// <summary>
    /// Gộp nhiều file PDF thành một theo đúng thứ tự đầu vào. File lỗi, hỏng, có mật khẩu
    /// hoặc không tồn tại bị bỏ qua và được báo lại trong kết quả.
    /// </summary>
    internal static class PdfMerger
    {
        public static PdfMergeResult Merge(IReadOnlyList<string> inputPaths, string outputPath)
        {
            var failed = new List<string>();
            var opened = new List<PdfDocument>();
            string? tempPath = null;

            try
            {
                using var output = new PdfDocument();
                int pagesWritten = 0;

                foreach (string path in inputPaths)
                {
                    try
                    {
                        if (!File.Exists(path))
                        {
                            failed.Add(path);
                            continue;
                        }

                        // Import: chỉ đọc trang; không có password provider nên file mã hóa ném lỗi, không bao giờ hỏi mật khẩu.
                        var source = PdfReader.Open(path, PdfDocumentOpenMode.Import);
                        opened.Add(source);

                        var pages = source.Pages.Cast<PdfPage>().ToList();
                        if (pages.Count == 0)
                        {
                            failed.Add(path);
                            continue;
                        }

                        foreach (var page in pages) output.AddPage(page);
                        pagesWritten += pages.Count;
                    }
                    catch (Exception)
                    {
                        failed.Add(path);
                    }
                }

                if (pagesWritten == 0)
                    return new PdfMergeResult(0, failed);

                string? outDir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                tempPath = Path.Combine(outDir ?? ".", $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
                output.Save(tempPath);
                File.Move(tempPath, outputPath, overwrite: true);
                tempPath = null;

                return new PdfMergeResult(pagesWritten, failed);
            }
            finally
            {
                foreach (var doc in opened)
                {
                    try { doc.Dispose(); } catch { }
                }
                if (tempPath != null)
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
    }
}
