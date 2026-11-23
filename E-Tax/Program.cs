namespace E_Tax
{
    /// <summary>Mỗi người dùng Windows chỉ chạy một bản, để hai tiến trình không cùng ghi vào kho và ghi chú.</summary>
    internal static class SingleInstance
    {
        public static string MutexName(string userSid) => $@"Global\E-Tax.SingleInstance.{userSid}";

        /// <summary>Trả về mutex đang giữ; null nếu đã có bản khác chạy. Phải giữ đối tượng này tới khi thoát.</summary>
        public static Mutex? TryAcquire(string name)
        {
            try
            {
                var mutex = new Mutex(true, name, out bool createdNew);
                if (createdNew) return mutex;
                mutex.Dispose();
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null; // mutex đã tồn tại và không mở được: coi như đang có bản khác chạy
            }
        }
    }

    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Phải cài trước khi tạo bất kỳ form nào
            AppErrorHandler.Install();
            // To customize application configuration such as set high DPI settings or default font,
           // see https://hoadondientu.gdt.gov.vn:30000/security-taxpayer/authenticate.
            ApplicationConfiguration.Initialize();

            string user = System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
            using var instance = SingleInstance.TryAcquire(SingleInstance.MutexName(user));
            if (instance == null)
            {
                MessageBox.Show("E-Tax đang chạy. Hãy dùng cửa sổ đã mở.", "E-Tax", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Application.Run(new Form1());
        }
    }
}