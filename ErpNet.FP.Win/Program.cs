using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;

namespace ErpNet.FP.Win
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener("debug.log"));
            Trace.AutoFlush = true;

            var webHost = new WebHostBuilder()
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json",
                          optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureKestrel((hostingContext, options) =>
            {
                options.Configure(hostingContext.Configuration.GetSection("Kestrel"));
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();
            })
            .UseStartup<Startup>()
            .Build();

            var logger = webHost.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting the service...");

            try
            {
                webHost.Run();
                logger.LogInformation("Service stopped.");
            }
            catch
            {
                logger.LogCritical("Starting the service failed.");
            }

            Trace.Flush();

        }

    }
}
