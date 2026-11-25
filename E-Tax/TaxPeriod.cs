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
        private static readonly Regex DayMonthPrefix = new(@"^\d{1,2}/\d{1,2}/\d{4}", RegexOptions.Compiled);
        private static readonly Regex TrailingOffset = new(@"\s*(?:Z|([+-])(\d{2}):?(\d{2}))$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly string[] DayMonthFormats = { "d/M/yyyy", "d/M/yyyy H:m", "d/M/yyyy H:m:s" };

        /// <summary>Ngày hiển thị dd/MM/yyyy theo giờ Việt Nam; không đọc được thì giữ nguyên chuỗi gốc.</summary>
        public static string? ToDisplay(string? raw) =>
            ToVietnamDate(raw) is DateTime date ? date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : raw;

        public static DateTime? ToVietnamDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            raw = raw.Trim();
            var ci = CultureInfo.InvariantCulture;

            if (DateTime.TryParseExact(raw, "yyyyMMddHHmmss", ci, DateTimeStyles.None, out var exact))
                return exact.Date;

            // Ngày có dấu "/" luôn là ngày/tháng; không rơi xuống parse chung vì sẽ bị đọc kiểu Mỹ
            if (DayMonthPrefix.IsMatch(raw))
                return ParseDayMonth(raw);

            if (HasOffset.IsMatch(raw))
            {
                if (DateTimeOffset.TryParse(raw, ci, DateTimeStyles.None, out var dto))
                    return dto.ToOffset(VietnamOffset).DateTime.Date;
                return null;
            }

            return DateTime.TryParse(raw, ci, DateTimeStyles.None, out var dt) ? dt.Date : null;
        }

        private static DateTime? ParseDayMonth(string raw)
        {
            var offsetMatch = TrailingOffset.Match(raw);
            string local = offsetMatch.Success ? raw.Substring(0, offsetMatch.Index).TrimEnd() : raw;
            if (!DateTime.TryParseExact(local, DayMonthFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return null;
            if (!offsetMatch.Success) return parsed.Date;

            var offset = TimeSpan.Zero;
            if (!offsetMatch.Value.Trim().Equals("Z", StringComparison.OrdinalIgnoreCase))
            {
                int hours = int.Parse(offsetMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                int minutes = int.Parse(offsetMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                if (hours > 14 || minutes > 59 || (hours == 14 && minutes > 0)) return null;
                offset = new TimeSpan(hours, minutes, 0);
                if (offsetMatch.Groups[1].Value == "-") offset = -offset;
            }
            return new DateTimeOffset(parsed, offset).ToOffset(VietnamOffset).DateTime.Date;
        }
    }
}
