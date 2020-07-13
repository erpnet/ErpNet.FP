namespace ErpNet.FP.Server
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Serilog;

    public class Program
    {
        private const string DebugLogFileName = "debug.log";
        // Serilog appends yyyyMMdd before file extension when rolling is set to daily
        private const string DebugLogFilePattern = "debug*.log";
        private const string AppSettingsFileName = "appsettings.json";

        public static IHostBuilder CreateHostBuilder(string pathToContentRoot, string[] args) =>
            Host.CreateDefaultBuilder(args)
#if Windows
            .UseWindowsService()
#endif
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;
                config.AddJsonFile(AppSettingsFileName, optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                .UseKestrel()
                .UseContentRoot(pathToContentRoot)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile(AppSettingsFileName, optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureKestrel((hostingContext, options) =>
                {
                    options.Configure(hostingContext.Configuration.GetSection("Kestrel"));

                    // Overriding some of the config values 
                    options.AllowSynchronousIO = true;
                    options.Limits.MaxRequestBodySize = 500 * 1024;
                })
                .UseStartup<Startup>()
                .UseSerilog();
            });

        public static void EnsureAppSettingsJson(string pathToContentRoot)
        {
            var appSettingsJsonFilePath = Path.Combine(pathToContentRoot, AppSettingsFileName);

            if (!File.Exists(appSettingsJsonFilePath))
            {
                var defaultAppSettings = @"{ ""ErpNet.FP"": {""AutoDetect"": true, ""Printers"": { } }, ""Kestrel"": { ""EndPoints"": { ""Http"": { ""Url"": ""http://0.0.0.0:8001"" } } } }";
                File.WriteAllText(appSettingsJsonFilePath, defaultAppSettings);
            }
        }

        private static Task EnsureDebugLogHistoryInBackground(string pathToContentRoot, CancellationToken ct)
        {
            return Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromDays(1), ct);

                    try
                    {
                        EnsureDebugLogHistory(pathToContentRoot);
                    }
                    catch (OperationCanceledException)
                    { break; }
                    catch (ThreadAbortException)
                    { break; }
                    catch
                    { }
                }
            });
        }

        private static string EnsureDebugLogHistory(string pathToContentRoot)
        {
            var debugLogFolder = Path.Combine(pathToContentRoot, "wwwroot", "debug");
            var debugLogFilePath = Path.Combine(debugLogFolder, DebugLogFileName);
            Directory.CreateDirectory(debugLogFolder);

            for (var i = 9; i > 1; i--)
            {
                if (File.Exists($"{debugLogFilePath}.{i - 1}.zip"))
                {
                    File.Move($"{debugLogFilePath}.{i - 1}.zip", $"{debugLogFilePath}.{i}.zip", true);
                }
            }

            var files = Directory.GetFiles(debugLogFolder, DebugLogFilePattern);
            if (files.Length > 0)
            {
                // Zip the files
                using (var zip = ZipFile.Open($"{debugLogFilePath}.1.zip", ZipArchiveMode.Create))
                {
                    foreach (var fileNameWithDatePattern in files)
                    {
                        zip.CreateEntryFromFile(fileNameWithDatePattern, Path.GetFileName(fileNameWithDatePattern));
                        File.Delete(fileNameWithDatePattern);
                    }
                }
                
            }
            return debugLogFilePath;
        }

        public static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return (version != null) ? version.ToString() : "unknown";
        }

        public static int Main(string[] args)
        {
            var pathToContentRoot = Directory.GetCurrentDirectory();

            var location = Assembly.GetExecutingAssembly().Location;
            pathToContentRoot = Path.GetDirectoryName(location) ?? pathToContentRoot;
            Directory.SetCurrentDirectory(pathToContentRoot);

            EnsureAppSettingsJson(pathToContentRoot);

            var logOutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

            var loggerConfiguration = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: logOutputTemplate)
                .WriteTo.File(
                    EnsureDebugLogHistory(pathToContentRoot),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: logOutputTemplate);

            if (Debugger.IsAttached)
            {
                loggerConfiguration.MinimumLevel.Debug();
            }
            else
            {
                loggerConfiguration.MinimumLevel.Information();
            }

            Log.Logger = loggerConfiguration.CreateLogger();

            // Setup debug logs
            try
            {
                var builder = CreateHostBuilder(
                    pathToContentRoot,
                    args.Where(arg => arg != "--console").ToArray());

                var host = builder.Build();

                Log.Information($"Starting the service, version {GetVersion()}...");

                var cts = new CancellationTokenSource();
                
                EnsureDebugLogHistoryInBackground(pathToContentRoot, cts.Token);

                host.Run();

                cts.Cancel();

                Log.Information("Stopping the service.");

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while creating debug.log file: {ex.Message}");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
