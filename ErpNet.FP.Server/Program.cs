using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace ErpNet.FP.Server
{
    public class Program
    {
        private static readonly string DebugLogFileName = @"debug.log";

        public static void Main()
        {
            FileStream traceStream;
            try
            {
                EnsureDebugLogHistory();
                traceStream = new FileStream(DebugLogFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error creating FileStream for trace file \"{0}\":" +
                    "\r\n{1}", DebugLogFileName, ex.Message);
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

        public static void EnsureDebugLogHistory()
        {
            if (File.Exists(DebugLogFileName))
            {
                for (var i = 9; i > 1; i--)
                {
                    if (File.Exists($"{DebugLogFileName}.{i - 1}.zip"))
                    {
                        File.Move($"{DebugLogFileName}.{i - 1}.zip", $"{DebugLogFileName}.{i}.zip", true);
                    }
                }
                // Zip the file
                using (var zip = ZipFile.Open($"{DebugLogFileName}.1.zip", ZipArchiveMode.Create))
                    zip.CreateEntryFromFile(DebugLogFileName, DebugLogFileName);
            }
        }

    }
}
