using System.Text.Json;

namespace E_Tax
{
    /// <summary>
    /// Cấu hình bí mật đọc từ appsettings.json cạnh exe (file này không commit lên git).
    /// </summary>
    internal sealed record AppSettings(string? BlacklistApiToken)
    {
        public static string DefaultPath { get; } = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        /// <summary>
        /// File thiếu hoặc hỏng → mọi giá trị null, không ném exception.
        /// </summary>
        public static AppSettings Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings((string?)null);
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"⚠️ Không đọc được file cấu hình {path}: {ex.Message}");
            }
            return new AppSettings((string?)null);
        }
    }
}
