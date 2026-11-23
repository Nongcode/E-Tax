using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace E_Tax
{
    /// <summary>Kỳ thuế (tháng, quý, năm hoặc tùy chọn) với khoảng ngày và nhãn hiển thị.</summary>
    internal readonly record struct TaxPeriod(DateTime From, DateTime To, string Label)
    {
        public static TaxPeriod Month(int year, int month)
        {
            if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));
            var from = new DateTime(year, month, 1);
            return new TaxPeriod(from, from.AddMonths(1).AddDays(-1), $"Tháng {month}/{year}");
        }

        public static TaxPeriod Quarter(int year, int quarter)
        {
            if (quarter < 1 || quarter > 4) throw new ArgumentOutOfRangeException(nameof(quarter));
            var from = new DateTime(year, (quarter - 1) * 3 + 1, 1);
            return new TaxPeriod(from, from.AddMonths(3).AddDays(-1), $"Quý {quarter}/{year}");
        }

        public static TaxPeriod Year(int year) =>
            new TaxPeriod(new DateTime(year, 1, 1), new DateTime(year, 12, 31), $"Năm {year}");

        public static TaxPeriod Custom(DateTime from, DateTime to) =>
            new TaxPeriod(from.Date, to.Date,
                $"{from.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} - {to.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}");
    }

    /// <summary>Chuẩn hóa ngày lập hóa đơn từ cổng thuế về ngày theo giờ Việt Nam (UTC+7).</summary>
    internal static class InvoiceDate
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static readonly Regex HasOffset = new(@"[T ]\S*(Z|[+-]\d{2}:?\d{2})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static DateTime? ToVietnamDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            raw = raw.Trim();
            var ci = CultureInfo.InvariantCulture;

            if (DateTime.TryParseExact(raw, new[] { "yyyyMMddHHmmss", "dd/MM/yyyy" }, ci, DateTimeStyles.None, out var exact))
                return exact.Date;

            if (HasOffset.IsMatch(raw))
            {
                if (DateTimeOffset.TryParse(raw, ci, DateTimeStyles.None, out var dto))
                    return dto.ToOffset(VietnamOffset).DateTime.Date;
                return null;
            }

            return DateTime.TryParse(raw, ci, DateTimeStyles.None, out var dt) ? dt.Date : null;
        }
    }
}
