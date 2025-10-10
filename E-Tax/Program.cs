namespace E_Tax
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
           // see https://hoadondientu.gdt.gov.vn:30000/security-taxpayer/authenticate.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}