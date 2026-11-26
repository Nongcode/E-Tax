using System.Windows.Forms;

namespace E_Tax
{
    /// <summary>Một cột trong cài đặt "Cột hiển thị": tên control cột và có hiện không. Thứ tự trong danh sách = thứ tự hiển thị.</summary>
    public sealed record ColumnState(string Name, bool Visible)
    {
        /// <summary>Chữ tiêu đề lúc chụp (chỉ để hiển thị trong hộp thoại, không lưu).</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string Header { get; init; } = "";
    }

    /// <summary>
    /// Đọc/áp thứ tự và ẩn hiện cột của DataGridView. Cột cố định (Frozen) và cột "STT" luôn hiện, luôn đứng đầu.
    /// Cột lạ trong cài đặt bị bỏ qua; cột mới chưa có trong cài đặt nối vào cuối, giữ trạng thái hiện tại.
    /// </summary>
    internal static class GridColumnLayout
    {
        public static bool IsLocked(DataGridViewColumn c) =>
            c.Frozen || string.Equals(c.HeaderText?.Trim(), "STT", StringComparison.OrdinalIgnoreCase);

        public static List<ColumnState> Capture(DataGridView grid) =>
            grid.Columns.Cast<DataGridViewColumn>()
                .OrderBy(c => c.DisplayIndex)
                .Select(c => new ColumnState(c.Name, c.Visible) { Header = c.HeaderText ?? c.Name })
                .ToList();

        public static void Apply(DataGridView grid, IEnumerable<ColumnState> states)
        {
            var byName = grid.Columns.Cast<DataGridViewColumn>().Where(c => !string.IsNullOrEmpty(c.Name))
                .GroupBy(c => c.Name).ToDictionary(g => g.Key, g => g.First());
            var saved = states.Where(s => byName.ContainsKey(s.Name)).GroupBy(s => s.Name).Select(g => g.First()).ToList();

            var locked = grid.Columns.Cast<DataGridViewColumn>().Where(IsLocked).OrderBy(c => c.DisplayIndex).ToList();
            var ordered = new List<DataGridViewColumn>(locked);
            ordered.AddRange(saved.Select(s => byName[s.Name]).Where(c => !locked.Contains(c)));
            ordered.AddRange(grid.Columns.Cast<DataGridViewColumn>().OrderBy(c => c.DisplayIndex).Where(c => !ordered.Contains(c)));

            for (int i = 0; i < ordered.Count; i++)
                ordered[i].DisplayIndex = i;

            var visible = saved.ToDictionary(s => s.Name, s => s.Visible);
            foreach (var c in ordered)
            {
                if (IsLocked(c)) c.Visible = true;
                else if (visible.TryGetValue(c.Name, out bool v)) c.Visible = v;
            }
            // Không để bảng trống trơn: luôn còn ít nhất một cột
            if (ordered.Count > 0 && ordered.All(c => !c.Visible)) ordered[0].Visible = true;
        }
    }
}
