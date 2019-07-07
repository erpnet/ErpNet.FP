using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;

namespace ErpNet.FP.Server
{
    public class Program
    {
        private static readonly string DebugLogFileName = @"debug.log";

        private static IEnumerable<IPAddress> GetLocalV4Addresses()
        {
            return from iface in NetworkInterface.GetAllNetworkInterfaces()
                   where iface.OperationalStatus == OperationalStatus.Up
                   from address in iface.GetIPProperties().UnicastAddresses
                   where address.Address.AddressFamily == AddressFamily.InterNetwork
                   select address.Address;
        }

        public static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetName().Version.ToString();
        }

        public static void Main()
        {
            var debugLogFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "debug");
            var debugLogFilePath = Path.Combine(debugLogFolder, DebugLogFileName);
            Directory.CreateDirectory(debugLogFolder);
            FileStream traceStream;
            try
            {
                EnsureDebugLogHistory();
                traceStream = new FileStream(debugLogFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error creating FileStream for trace file \"{0}\":" +
                    "\r\n{1}", debugLogFilePath, ex.Message);
                return;
            }

            // Create a TextWriterTraceListener object that takes a stream.
            TextWriterTraceListener textListener;
            textListener = new TextWriterTraceListener(traceStream);
            Trace.Listeners.Add(textListener);
            Trace.AutoFlush = true;

            Trace.WriteLine($"Starting the application, version {GetVersion()}...");

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
            var debugLogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "debug", DebugLogFileName);
            if (File.Exists(debugLogFilePath))
            {
                for (var i = 9; i > 1; i--)
                {
                    if (File.Exists($"{debugLogFilePath}.{i - 1}.zip"))
                    {
                        File.Move($"{debugLogFilePath}.{i - 1}.zip", $"{debugLogFilePath}.{i}.zip", true);
                    }
                }
                // Zip the file
                using (var zip = ZipFile.Open($"{debugLogFilePath}.1.zip", ZipArchiveMode.Create))
                    zip.CreateEntryFromFile(debugLogFilePath, DebugLogFileName);
            }
        }

    }
}
