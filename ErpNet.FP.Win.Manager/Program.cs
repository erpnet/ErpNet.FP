using ErpNet.FP.Server.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ErpNet.FP.Win.Manager
{
    static class Program
    {
        private static IServiceProvider ServiceProvider { get; set; }

        static void ConfigureServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            services.ConfigureWritable<ErpNetFPConfigOptions>(configuration.GetSection("ErpNet.FP"));
            ServiceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();

            var builder = new ConfigurationBuilder()
              .AddJsonFile(@"appsettings.json", optional: true, reloadOnChange: true);

            var configuration = builder.Build();
            ConfigureServices(configuration);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(true);
            Application.Run(new MainForm());
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetProcessDPIAware();
    }
}