using System;
using System.Collections.Generic;
using System.IO;
using E_Tax;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Xunit;

namespace E_Tax.Tests
{
    public class PdfMergerTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "PdfMergerTests_" + Guid.NewGuid());

        public PdfMergerTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch { }
        }

        private string MakePdf(string name, int pages, string? password = null)
        {
            string path = Path.Combine(_dir, name);
            using var doc = new PdfDocument();
            for (int i = 0; i < pages; i++) doc.AddPage();
            if (password != null)
            {
                doc.SecuritySettings.OwnerPassword = password;
                doc.SecuritySettings.UserPassword = password;
            }
            doc.Save(path);
            return path;
        }

        private static int PageCount(string path)
        {
            using var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import);
            return doc.PageCount;
        }

        [Fact]
        public void Merge_KeepsOrderAndPageCount()
        {
            string a = MakePdf("a.pdf", 2);
            string b = MakePdf("b.pdf", 3);
            string output = Path.Combine(_dir, "out.pdf");

            var result = PdfMerger.Merge(new[] { a, b }, output);

            Assert.Equal(5, result.PagesWritten);
            Assert.Empty(result.FailedFiles);
            Assert.Equal(5, PageCount(output));
            Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
        }

        [Fact]
        public void Merge_CorruptFile_SkipsAndReports()
        {
            string a = MakePdf("a.pdf", 2);
            string corrupt = Path.Combine(_dir, "corrupt.pdf");
            File.WriteAllText(corrupt, "đây không phải PDF");
            string output = Path.Combine(_dir, "out.pdf");

            var result = PdfMerger.Merge(new[] { corrupt, a }, output);

            Assert.Equal(new[] { corrupt }, result.FailedFiles);
            Assert.Equal(2, result.PagesWritten);
            Assert.Equal(2, PageCount(output));
        }

        [Fact]
        public void Merge_PasswordProtected_SkipsAndReports()
        {
            string a = MakePdf("a.pdf", 1);
            string locked = MakePdf("locked.pdf", 2, "bimat");
            string output = Path.Combine(_dir, "out.pdf");

            var result = PdfMerger.Merge(new[] { a, locked }, output);

            Assert.Equal(new[] { locked }, result.FailedFiles);
            Assert.Equal(1, result.PagesWritten);
        }

        [Fact]
        public void Merge_MissingFile_Reports()
        {
            string missing = Path.Combine(_dir, "khong-co.pdf");
            string a = MakePdf("a.pdf", 1);
            string output = Path.Combine(_dir, "out.pdf");

            var result = PdfMerger.Merge(new[] { missing, a }, output);

            Assert.Equal(new[] { missing }, result.FailedFiles);
            Assert.Equal(1, result.PagesWritten);
        }

        [Fact]
        public void Merge_NoValidInput_DoesNotReplaceExistingOutput()
        {
            string corrupt = Path.Combine(_dir, "corrupt.pdf");
            File.WriteAllText(corrupt, "hỏng");
            string output = Path.Combine(_dir, "out.pdf");
            File.WriteAllBytes(output, new byte[] { 1, 2, 3, 4 });
            byte[] oldBytes = File.ReadAllBytes(output);

            var result = PdfMerger.Merge(new[] { corrupt }, output);

            Assert.Equal(0, result.PagesWritten);
            Assert.Equal(oldBytes, File.ReadAllBytes(output));
            Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
        }
    }
}
