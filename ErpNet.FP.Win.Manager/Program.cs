using ErpNet.FP.Server.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
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

            services.AddOptions();
            services.ConfigureWritable<ErpNetFPConfigOptions>(configuration.GetSection("ErpNet.FP"));
            services.AddSingleton<IOptionsMonitor<ErpNetFPConfigOptions>, OptionsMonitor<ErpNetFPConfigOptions>>();
            services.AddSingleton<IMainForm, MainForm>();

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
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile(@"appsettings.json", optional: true, reloadOnChange: true);

            var configuration = builder.Build();
            ConfigureServices(configuration);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(true);
            ServiceProvider.GetService<IOptionsMonitor<ErpNetFPConfigOptions>>();
            Application.Run((MainForm)ServiceProvider.GetService<IMainForm>());
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetProcessDPIAware();
    }
}