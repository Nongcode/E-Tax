using System.Drawing;
using System.Windows.Forms;

namespace E_Tax
{
    /// <summary>
    /// Hộp thoại "Cột hiển thị" của bánh răng ở dải tab: tick để ẩn/hiện, ↑↓ hoặc kéo thả để đổi thứ tự, "Mặc định" trả về như ban đầu.
    /// Cột khóa (STT/cột cố định) luôn hiện và luôn đứng đầu. Kết quả đọc ở <see cref="Result"/> khi DialogResult = OK.
    /// </summary>
    internal sealed class ColumnChooserForm : Form
    {
        private readonly CheckedListBox _list = new()
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            IntegralHeight = false,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 10.5F),
            AllowDrop = true
        };
        private readonly IReadOnlyList<ColumnState> _defaults;
        private readonly ISet<string> _locked;
        private List<ColumnState> _items;
        private bool _syncing;
        private int _dragFrom = -1;

        public ColumnChooserForm(string gridTitle, IReadOnlyList<ColumnState> current, IReadOnlyList<ColumnState> defaults, ISet<string> locked)
        {
            _defaults = defaults;
            _locked = locked;
            _items = Normalize(current);

            Text = "Cột hiển thị — " + gridTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(380, 470);
            Font = UiTheme.BaseFont;
            BackColor = UiTheme.Surface;

            var hint = new Label
            {
                Text = "Tick để hiện cột. Kéo thả hoặc ↑ ↓ để đổi thứ tự.",
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(12, 10, 12, 0),
                ForeColor = UiTheme.MutedText
            };
            var up = new Button { Text = "↑ Lên", Size = new Size(84, 30), Margin = new Padding(0, 0, 0, 6) };
            var down = new Button { Text = "↓ Xuống", Size = new Size(84, 30) };
            up.Click += (_, _) => MoveSelected(-1);
            down.Click += (_, _) => MoveSelected(+1);
            var side = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 100, FlowDirection = FlowDirection.TopDown, Padding = new Padding(8, 0, 8, 0) };
            side.Controls.Add(up);
            side.Controls.Add(down);
            var listBox = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 0, 0) };
            listBox.Controls.Add(_list);

            var reset = new Button { Text = "Mặc định", Size = new Size(90, 30) };
            var cancel = new Button { Text = "Hủy", Size = new Size(80, 30), DialogResult = DialogResult.Cancel };
            var ok = new Button { Text = "Áp dụng", Size = new Size(90, 30), DialogResult = DialogResult.OK };
            reset.Click += (_, _) => ResetToDefault();
            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8, 8, 8, 0) };
            footer.Controls.AddRange(new Control[] { ok, cancel, reset });
            reset.Margin = new Padding(3, 3, 80, 3);

            Controls.Add(listBox);
            Controls.Add(side);
            Controls.Add(hint);
            Controls.Add(footer);
            AcceptButton = ok;
            CancelButton = cancel;

            _list.ItemCheck += (_, e) =>
            {
                if (_syncing) return;
                if (_locked.Contains(_items[e.Index].Name)) { e.NewValue = CheckState.Checked; return; }
                _items[e.Index] = _items[e.Index] with { Visible = e.NewValue == CheckState.Checked };
            };
            _list.MouseDown += (_, e) => _dragFrom = _list.IndexFromPoint(e.Location);
            _list.MouseMove += (_, e) =>
            {
                if (e.Button == MouseButtons.Left && _dragFrom >= 0 && !_locked.Contains(_items[_dragFrom].Name))
                    _list.DoDragDrop(_dragFrom, DragDropEffects.Move);
            };
            _list.DragOver += (_, e) => e.Effect = DragDropEffects.Move;
            _list.DragDrop += (_, e) =>
            {
                int to = _list.IndexFromPoint(_list.PointToClient(new Point(e.X, e.Y)));
                if (_dragFrom < 0 || to < 0) return;
                MoveTo(_dragFrom, to);
                _dragFrom = -1;
            };

            UiTheme.ApplyTo(this);
            _list.Font = new Font("Segoe UI", 10.5F); // ApplyTo đặt font form 9pt; danh sách cần thoáng hơn
            Refill(0);
        }

        /// <summary>Trạng thái hiện tại trong hộp thoại (thứ tự + ẩn hiện).</summary>
        public IReadOnlyList<ColumnState> Result => _items.ToList();

        internal void Select(int index) => _list.SelectedIndex = index;

        internal void SetChecked(int index, bool value) => _list.SetItemChecked(index, value);

        internal void MoveSelected(int delta)
        {
            int from = _list.SelectedIndex;
            if (from < 0) return;
            MoveTo(from, from + delta);
        }

        internal void ResetToDefault()
        {
            _items = Normalize(_defaults);
            Refill(Math.Max(0, _list.SelectedIndex));
        }

        private void MoveTo(int from, int to)
        {
            int firstMovable = _items.TakeWhile(i => _locked.Contains(i.Name)).Count();
            if (from < firstMovable || to < firstMovable || to >= _items.Count || from == to) return;
            var item = _items[from];
            _items.RemoveAt(from);
            _items.Insert(to, item);
            Refill(to);
        }

        /// <summary>Cột khóa luôn đứng đầu và luôn hiện.</summary>
        private List<ColumnState> Normalize(IEnumerable<ColumnState> states)
        {
            var list = states.ToList();
            var locked = list.Where(s => _locked.Contains(s.Name)).Select(s => s with { Visible = true });
            return locked.Concat(list.Where(s => !_locked.Contains(s.Name))).ToList();
        }

        private void Refill(int selected)
        {
            _syncing = true;
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var s in _items)
                _list.Items.Add(string.IsNullOrEmpty(s.Header) ? s.Name : s.Header + (_locked.Contains(s.Name) ? "  (luôn hiện)" : ""), s.Visible);
            _list.EndUpdate();
            if (_items.Count > 0) _list.SelectedIndex = Math.Clamp(selected, 0, _items.Count - 1);
            _syncing = false;
        }
    }
}
