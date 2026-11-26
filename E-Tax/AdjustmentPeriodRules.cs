using System.Globalization;

namespace E_Tax
{
    internal enum CrossPeriodAction
    {
        /// <summary>Mua vào điều chỉnh tăng thuế được khấu trừ: khai kỳ hiện tại ở [38] (cả sửa sai lẫn nghiệp vụ phát sinh).</summary>
        CurrentPeriodCt38,
        /// <summary>Mua vào điều chỉnh giảm thuế được khấu trừ: [37] kỳ này hoặc khai bổ sung kỳ gốc, kế toán chọn.</summary>
        PurchaseDecrease,
        /// <summary>Bán ra điều chỉnh: nghiệp vụ phát sinh tính kỳ này, sửa sai khai bổ sung kỳ gốc; kế toán chọn.</summary>
        SalesAdjustment,
        /// <summary>HĐ thay thế: chưa tính được chênh lệch so với HĐ gốc, kế toán tự xác định.</summary>
        Replacement
    }

    internal sealed record CrossPeriodInvoice(string Direction, string Label, DateTime OriginalDate, decimal Amount, decimal Tax, CrossPeriodAction Action);

    internal sealed record PeriodSplit(IReadOnlyList<InvoiceDetailItem> Current, decimal Ct38, IReadOnlyList<CrossPeriodInvoice> Moved)
    {
        /// <summary>Ghi chú cho màn hình và file xuất: nhóm HĐ được tách khỏi số liệu kỳ này, lý do và cách xử lý gợi ý.</summary>
        public IReadOnlyList<string> Notes()
        {
            var notes = new List<string>();
            void Add(CrossPeriodAction action, Func<int, string> text)
            {
                var group = Moved.Where(m => m.Action == action).ToList();
                if (group.Count > 0) notes.Add(text(group.Count) + $" ({List(group)}).");
            }
            Add(CrossPeriodAction.CurrentPeriodCt38, n =>
                $"Có {n} hóa đơn mua vào điều chỉnh tăng thuế được khấu trừ, hóa đơn gốc ở kỳ trước: không cộng vào [23]/[24], " +
                $"đã điền {Fmt(Ct38)} vào [38] kỳ này");
            Add(CrossPeriodAction.PurchaseDecrease, n =>
                $"Có {n} hóa đơn mua vào điều chỉnh giảm thuế được khấu trừ, hóa đơn gốc ở kỳ khác, chưa tính vào tờ khai kỳ này. " +
                "Nếu do chiết khấu, trả hàng (khoản 5 Điều 10 TT 91/2026) hoặc chỉ làm giảm [43] kỳ gốc thì ghi [37] kỳ này; " +
                "nếu làm tăng thuế phải nộp của kỳ gốc thì khai bổ sung kỳ gốc");
            Add(CrossPeriodAction.SalesAdjustment, n =>
                $"Có {n} hóa đơn bán ra điều chỉnh, hóa đơn gốc ở kỳ khác, chưa tính vào tờ khai kỳ này. " +
                "Nếu do nghiệp vụ phát sinh (khoản 5 Điều 10 TT 91/2026) thì kê khai kỳ này; nếu sửa sai thì khai bổ sung kỳ gốc");
            Add(CrossPeriodAction.Replacement, n =>
                $"Có {n} hóa đơn thay thế, hóa đơn gốc ở kỳ khác, chưa tính vào tờ khai kỳ này. " +
                "Kế toán so với hóa đơn gốc để chọn khai bổ sung kỳ gốc hay [37]/[38] kỳ này");
            return notes;
        }

        private static string List(IEnumerable<CrossPeriodInvoice> items) =>
            string.Join("; ", items.Select(m => $"{m.Direction} {m.Label}, HĐ gốc {m.OriginalDate:dd/MM/yyyy}, chưa thuế {Fmt(m.Amount)}, thuế {Fmt(m.Tax)}"));

        private static string Fmt(decimal v) => v.ToString("#,##0", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Kỳ kê khai của HĐ điều chỉnh/thay thế có HĐ gốc thuộc kỳ khác (TT 91/2026 Điều 10, TT 89/2026 Phụ lục II).
    /// Dữ liệu cổng không cho biết điều chỉnh do sửa sai hay do nghiệp vụ phát sinh, và app không có tờ khai kỳ gốc,
    /// nên chỉ tự xếp trường hợp chắc chắn: mua vào tăng thuế được khấu trừ vào [38] kỳ này. Các trường hợp khác được
    /// tách khỏi số liệu kỳ này và liệt kê kèm gợi ý. Số tiền HĐ điều chỉnh mang dấu (tăng dương, giảm âm).
    /// HĐ điều chỉnh/thay thế có HĐ gốc cùng kỳ hoặc không rõ ngày gốc: tính bình thường.
    /// </summary>
    internal static class AdjustmentPeriodRules
    {
        public static PeriodSplit Split(IEnumerable<InvoiceDetailItem> items, IEnumerable<SearchResult> headers, TaxPeriod period)
        {
            var itemList = items.Where(i => i != null).ToList();
            var headerList = headers.Where(h => h != null).ToList();
            var links = InvoiceLinker.Link(headerList);
            var moved = new List<CrossPeriodInvoice>();
            var movedKeys = new HashSet<(string, string)>();
            decimal ct38 = 0m;

            foreach (var h in headerList)
            {
                var relation = InvoiceClassifier.RelationOf(h.Trang_thai_hoa_don);
                if (relation != InvoiceRelation.ThayThe && relation != InvoiceRelation.DieuChinh) continue;
                InvoiceLink? link = null;
                if (h.Id != null) links.TryGetValue(h.Id, out link);
                if ((link?.OriginalDate ?? InvoiceDate.ToVietnamDate(h.Ngay_lap_goc)) is not DateTime original) continue;
                if (original >= period.From.Date && original <= period.To.Date) continue;

                var key = (h.Loai_hoa_don ?? "", InvoiceSearchResults.KeyOf(h));
                if (!movedKeys.Add(key)) continue;
                var lines = itemList.Where(l => (l.LoaiHoaDon ?? "", l.InvoiceKey ?? "") == key).ToList();
                decimal amount = lines.Sum(l => l.DoanhSoChuaThue ?? 0m);
                decimal tax = lines.Sum(l => l.TienThueGTGT ?? 0m);
                bool purchase = h.Loai_hoa_don == TaxListing.DirectionLabel(InvoiceType.Bought);

                CrossPeriodAction action = relation == InvoiceRelation.ThayThe ? CrossPeriodAction.Replacement
                    : !purchase ? CrossPeriodAction.SalesAdjustment
                    : tax > 0m ? CrossPeriodAction.CurrentPeriodCt38
                    : CrossPeriodAction.PurchaseDecrease;
                if (action == CrossPeriodAction.CurrentPeriodCt38) ct38 += tax;
                moved.Add(new CrossPeriodInvoice(h.Loai_hoa_don ?? "", $"{h.Ky_hieu_hoa_don}-{h.So_hoa_don}", original, amount, tax, action));
            }

            var current = itemList.Where(l => !movedKeys.Contains((l.LoaiHoaDon ?? "", l.InvoiceKey ?? ""))).ToList();
            return new PeriodSplit(current, ct38, moved);
        }
    }
}
