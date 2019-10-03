namespace ErpNet.FP.Server
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;

    public class Program
    {
        private static readonly string DebugLogFileName = @"debug.log";
        private static readonly string AppSettingsFileName = @"appsettings.json";

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
                    options.AllowSynchronousIO = false;
                    options.Limits.MaxRequestBodySize = 500 * 1024;
                })
                .UseStartup<Startup>();
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


        public static string EnsureDebugLogHistory(string pathToContentRoot)
        {
            var debugLogFolder = Path.Combine(pathToContentRoot, "wwwroot", "debug");
            var debugLogFilePath = Path.Combine(debugLogFolder, DebugLogFileName);
            Directory.CreateDirectory(debugLogFolder);
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
                File.Delete(debugLogFilePath);
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

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: logOutputTemplate)
                .WriteTo.File(
                    EnsureDebugLogHistory(pathToContentRoot),
                    rollingInterval: RollingInterval.Infinite,
                    outputTemplate: logOutputTemplate)
                .CreateLogger();

            // Setup debug logs
            try
            {
                var builder = CreateHostBuilder(
                    pathToContentRoot,
                    args.Where(arg => arg != "--console").ToArray());

                var host = builder.Build();

                Log.Information($"Starting the service, version {GetVersion()}...");

                host.Run();

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
