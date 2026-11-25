namespace E_Tax
{
    /// <summary>
    /// Tiến trình của một tác vụ hàng loạt: bước hiện tại, số đã xong, tổng số, số lỗi.
    /// </summary>
    internal readonly record struct BatchProgress(string Step, int Done, int Total, int Failed)
    {
        public string Summary => Failed > 0
            ? $"{Step}: {Done}/{Total}, lỗi {Failed}"
            : $"{Step}: {Done}/{Total}";
    }
}
