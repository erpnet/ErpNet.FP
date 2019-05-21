using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;

namespace ErpNet.FP.Server
{
    public class Program
    {
        public static void Main()
        {
            FileStream traceStream;
            try
            {
                traceStream = new FileStream(@"debug.log", FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error creating FileStream for trace file \"{0}\":" +
                    "\r\n{1}", @"debug.log", ex.Message);
                return;
            }

            // Create a TextWriterTraceListener object that takes a stream.
            TextWriterTraceListener textListener;
            textListener = new TextWriterTraceListener(traceStream);
            Trace.Listeners.Add(textListener);
            Trace.AutoFlush = true;
            Trace.WriteLine("Starting the application...");

            var webHost = new WebHostBuilder()
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
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

            Trace.WriteLine("Stopping the application.");
            Trace.Flush();
        }

    }
}
