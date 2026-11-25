namespace E_Tax
{
    /// <summary>
    /// Không có trình duyệt Chromium để chuyển HTML sang PDF (tải lỗi do mạng, tường lửa, quyền ghi...).
    /// Thông báo hiển thị thẳng cho người dùng; không chứa URL.
    /// </summary>
    internal sealed class ChromiumUnavailableException : Exception
    {
        public const string UserMessage =
            "Không tải được trình duyệt Chromium để chuyển PDF. Vui lòng kiểm tra kết nối mạng hoặc tường lửa rồi thử lại.";

        public ChromiumUnavailableException(Exception? inner = null) : base(UserMessage, inner) { }

        /// <summary>
        /// Chạy bước tải Chromium; mọi lỗi (trừ hủy do chính người dùng) hoặc đường dẫn rỗng đều thành <see cref="ChromiumUnavailableException"/>.
        /// </summary>
        public static async Task<string> RunAsync(Func<Task<string?>> step, CancellationToken userToken, Action<Exception>? onFailure = null)
        {
            try
            {
                string? path = await step();
                if (string.IsNullOrEmpty(path))
                    throw new ChromiumUnavailableException();
                return path;
            }
            catch (ChromiumUnavailableException)
            {
                throw;
            }
            catch (OperationCanceledException) when (userToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                onFailure?.Invoke(ex);
                throw new ChromiumUnavailableException(ex);
            }
        }
    }
}
