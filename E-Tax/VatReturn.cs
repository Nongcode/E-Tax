using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace E_Tax
{
    /// <summary>Số liệu nhập tay cho tờ khai 01/GTGT dự thảo (các chỉ tiêu không tính được từ bảng kê).</summary>
    internal sealed record VatReturnInput(decimal CT22, decimal CT37, decimal CT38, decimal CT39a, decimal CT40b,
        decimal CT42, decimal CT23a, decimal CT24a, decimal CT25Adjust);

    /// <summary>Kết quả tính tờ khai dự thảo: giá trị theo mã chỉ tiêu (không ngoặc vuông) và các cảnh báo.</summary>
    internal sealed record VatReturn(IReadOnlyDictionary<string, decimal> Values, IReadOnlyList<string> Warnings);

    /// <summary>Tính các chỉ tiêu 01/GTGT (TT 80/2021) từ bảng kê mua vào/bán ra. Chỉ là bản dự thảo, chưa đối chiếu mẫu gốc.</summary>
    internal static class VatReturnBuilder
    {
        public static readonly IReadOnlyList<string> Codes = new[]
        {
            "22", "23", "23a", "24", "24a", "25", "26", "27", "28", "29", "30", "31", "32", "32a", "33",
            "34", "35", "36", "37", "38", "39a", "40a", "40b", "40", "41", "42", "43"
        };

        private static readonly HashSet<string> KnownRates = new() { "KCT", "KKKNT", "0%", "5%", "8%", "10%" };

        public static VatReturn Build(IReadOnlyList<TaxListingGroup> bought, IReadOnlyList<TaxListingGroup> sold, VatReturnInput input)
        {
            var warnings = new List<string>();

            // Mua vào: [23]/[24] cộng mọi thuế suất đã biết
            decimal boughtAmount = 0m, boughtTax = 0m;
            foreach (var g in bought)
            {
                if (!KnownRates.Contains(g.ThueSuat))
                {
                    warnings.Add(UnknownRateWarning("mua vào", g));
                    continue;
                }
                boughtAmount += g.TongDoanhSo;
                boughtTax += g.TongThue;
            }

            // Bán ra
            decimal ct26 = 0m, ct29 = 0m, ct30 = 0m, ct31 = 0m, ct32 = 0m, ct32a = 0m, ct33 = 0m;
            void WarnTax(TaxListingGroup g)
            {
                if (g.TongThue != 0m)
                    warnings.Add($"Hàng bán ra thuế suất {g.ThueSuat} có thuế {Fmt(g.TongThue)} nhưng không được tính vào [28].");
            }
            foreach (var g in sold)
            {
                switch (g.ThueSuat)
                {
                    case "KCT": ct26 += g.TongDoanhSo; WarnTax(g); break;
                    case "KKKNT": ct32a += g.TongDoanhSo; WarnTax(g); break;
                    case "0%": ct29 += g.TongDoanhSo; WarnTax(g); break;
                    case "5%": ct30 += g.TongDoanhSo; ct31 += g.TongThue; break;
                    case "8%":
                        ct32 += g.TongDoanhSo; ct33 += g.TongThue;
                        warnings.Add("Hàng bán ra thuế suất 8% đang được đưa vào [32]/[33] theo số tiền trên hóa đơn. " +
                                     "Cần kiểm tra với HTKK và các quy định giảm thuế hiện hành trước khi dùng.");
                        break;
                    case "10%": ct32 += g.TongDoanhSo; ct33 += g.TongThue; break;
                    default:
                        warnings.Add(UnknownRateWarning("bán ra", g));
                        break;
                }
            }

            int otherStatus = bought.Concat(sold).SelectMany(g => g.Rows).Count(r => r.TrangThai != 1);
            if (otherStatus > 0)
                warnings.Add($"Có {otherStatus} dòng thuộc hóa đơn có trạng thái khác 1 hoặc không rõ; các dòng này đã được tính vào tổng, cần kiểm tra lại.");

            if (bought.SelectMany(g => g.Rows).Any(r => r.ThieuSoTien))
                warnings.Add("Bảng kê mua vào có dòng thiếu số tiền nên [23]/[24] có thể bị thiếu.");
            if (sold.SelectMany(g => g.Rows).Any(r => r.ThieuSoTien))
                warnings.Add("Bảng kê bán ra có dòng thiếu số tiền nên các chỉ tiêu bán ra có thể bị thiếu.");

            var negatives = new List<string>();
            void Check(string code, decimal v) { if (v < 0m) negatives.Add($"[{code}]"); }
            Check("22", input.CT22); Check("23a", input.CT23a); Check("24a", input.CT24a); Check("37", input.CT37);
            Check("38", input.CT38); Check("39a", input.CT39a); Check("40b", input.CT40b); Check("42", input.CT42);
            if (negatives.Count > 0)
                warnings.Add("Số nhập tay âm ở chỉ tiêu " + string.Join(", ", negatives) + "; thông thường các chỉ tiêu này không âm, hãy kiểm tra lại.");

            decimal ct23 = boughtAmount + input.CT23a;
            decimal ct24 = boughtTax + input.CT24a;
            decimal ct25 = ct24 + input.CT25Adjust;
            decimal ct27 = ct29 + ct30 + ct32 + ct32a;
            decimal ct28 = ct31 + ct33;
            decimal ct34 = ct26 + ct27;
            decimal ct35 = ct28;
            decimal ct36 = ct35 - ct25;
            decimal x = ct36 - input.CT22 + input.CT37 - input.CT38 - input.CT39a;
            decimal ct40a = Math.Max(x, 0m);
            decimal ct40 = Math.Max(ct40a - input.CT40b, 0m);
            decimal ct41 = Math.Max(-x, 0m);
            decimal ct43 = ct41 - input.CT42;

            if (input.CT42 > ct41)
                warnings.Add($"[42] ({Fmt(input.CT42)}) lớn hơn [41] ({Fmt(ct41)}): số đề nghị hoàn vượt số chưa được khấu trừ hết.");
            if (input.CT40b > ct40a)
                warnings.Add($"[40b] ({Fmt(input.CT40b)}) lớn hơn [40a] ({Fmt(ct40a)}): [40] được tính bằng 0, phần vượt không được chuyển đi.");

            var values = new Dictionary<string, decimal>
            {
                ["22"] = input.CT22, ["23"] = ct23, ["23a"] = input.CT23a, ["24"] = ct24, ["24a"] = input.CT24a,
                ["25"] = ct25, ["26"] = ct26, ["27"] = ct27, ["28"] = ct28, ["29"] = ct29, ["30"] = ct30,
                ["31"] = ct31, ["32"] = ct32, ["32a"] = ct32a, ["33"] = ct33, ["34"] = ct34, ["35"] = ct35,
                ["36"] = ct36, ["37"] = input.CT37, ["38"] = input.CT38, ["39a"] = input.CT39a, ["40a"] = ct40a,
                ["40b"] = input.CT40b, ["40"] = ct40, ["41"] = ct41, ["42"] = input.CT42, ["43"] = ct43
            };
            return new VatReturn(values, warnings);
        }

        private static string UnknownRateWarning(string side, TaxListingGroup g) =>
            $"Bảng kê {side} có thuế suất \"{g.ThueSuat}\" không được cộng vào tờ khai " +
            $"(doanh số {Fmt(g.TongDoanhSo)}, thuế {Fmt(g.TongThue)}). Hãy kiểm tra lại.";

        private static string Fmt(decimal v) => v.ToString("#,##0.############", CultureInfo.InvariantCulture);

        /// <summary>Diễn giải tiếng Việt của từng chỉ tiêu.</summary>
        public static string Describe(string code) => code switch
        {
            "22" => "Thuế GTGT còn được khấu trừ kỳ trước chuyển sang",
            "23" => "Giá trị hàng hóa, dịch vụ mua vào",
            "23a" => "Trong đó: hàng hóa, dịch vụ nhập khẩu (nhập tay, không có trong bảng kê; cộng vào [23])",
            "24" => "Thuế GTGT của hàng hóa, dịch vụ mua vào",
            "24a" => "Trong đó: hàng hóa, dịch vụ nhập khẩu (nhập tay, không có trong bảng kê; cộng vào [24])",
            "25" => "Tổng số thuế GTGT được khấu trừ kỳ này",
            "26" => "Hàng hóa, dịch vụ bán ra không chịu thuế GTGT",
            "27" => "Tổng doanh thu hàng hóa, dịch vụ bán ra chịu thuế",
            "28" => "Thuế GTGT của hàng hóa, dịch vụ bán ra",
            "29" => "Hàng hóa, dịch vụ bán ra chịu thuế suất 0%",
            "30" => "Hàng hóa, dịch vụ bán ra chịu thuế suất 5% - doanh số",
            "31" => "Hàng hóa, dịch vụ bán ra chịu thuế suất 5% - thuế",
            "32" => "Hàng hóa, dịch vụ bán ra chịu thuế suất 10% và 8% - doanh số",
            "32a" => "Hàng hóa, dịch vụ bán ra không tính thuế (KKKNT)",
            "33" => "Hàng hóa, dịch vụ bán ra chịu thuế suất 10% và 8% - thuế",
            "34" => "Tổng doanh thu hàng hóa, dịch vụ bán ra",
            "35" => "Tổng thuế GTGT của hàng hóa, dịch vụ bán ra",
            "36" => "Thuế GTGT phát sinh trong kỳ ([35] - [25])",
            "37" => "Điều chỉnh giảm thuế GTGT còn được khấu trừ của các kỳ trước",
            "38" => "Điều chỉnh tăng thuế GTGT còn được khấu trừ của các kỳ trước",
            "39a" => "Thuế GTGT nhận bàn giao được khấu trừ trong kỳ",
            "40a" => "Thuế GTGT phải nộp của hoạt động sản xuất kinh doanh trong kỳ",
            "40b" => "Thuế GTGT mua vào của dự án đầu tư được bù trừ với thuế GTGT còn phải nộp của hoạt động sản xuất kinh doanh cùng kỳ tính thuế",
            "40" => "Thuế GTGT còn phải nộp trong kỳ",
            "41" => "Thuế GTGT chưa được khấu trừ hết kỳ này",
            "42" => "Thuế GTGT đề nghị hoàn",
            "43" => "Thuế GTGT còn được khấu trừ chuyển kỳ sau ([41] - [42])",
            _ => code
        };
    }
}
