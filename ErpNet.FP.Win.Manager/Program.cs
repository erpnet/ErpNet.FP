using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ErpNet.FP.Win.Manager
{
    static class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetProcessDPIAware();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(true);
            Application.Run(new MainForm());
        }
    }
}