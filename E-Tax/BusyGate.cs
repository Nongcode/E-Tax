namespace E_Tax
{
    /// <summary>Cổng bận chung: tại một thời điểm chỉ một luồng dài (tìm kiếm, tải, kho...) được chạy.</summary>
    internal sealed class BusyGate
    {
        private readonly object _lock = new();
        private string? _current;

        public event Action? Changed;

        public bool IsBusy { get { lock (_lock) return _current != null; } }

        public string? CurrentFlow { get { lock (_lock) return _current; } }

        public bool TryEnter(string flowName)
        {
            lock (_lock)
            {
                if (_current != null) return false;
                _current = flowName ?? "";
            }
            Changed?.Invoke();
            return true;
        }

        public void Exit()
        {
            lock (_lock)
            {
                if (_current == null) throw new InvalidOperationException("Thoát cổng bận khi chưa vào.");
                _current = null;
            }
            Changed?.Invoke();
        }
    }
}
