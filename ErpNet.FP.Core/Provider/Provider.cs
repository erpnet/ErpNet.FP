#nullable enable
namespace ErpNet.FP.Core.Provider
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using ErpNet.FP.Core.Configuration;
    using ErpNet.FP.Core.Drivers;
    using Serilog;
    using static ErpNet.FP.Core.Transports.ComTransport;

    /// <summary>
    /// General functions for finding and connecting fiscal printers.
    /// </summary>
    public class Provider
    {
        public Provider(ServiceOptions serviceOptions)
        {
            this.ServiceOptions = serviceOptions;
        }
        /// <summary>
        /// Adds the specified protocol to the provider.
        /// A protocol consists of a printer driver and a transport.
        /// </summary>
        /// <param name="driver">The driver.</param>
        /// <param name="transport">The transport.</param>
        public Provider Register(FiscalPrinterDriver driver, Transport transport)
        {
            var key = driver.DriverName + "." + transport.TransportName;
            protocols.Add(key, (driver, transport));
            return this;
        }

        public async Task<IFiscalPrinter?> DetectPrinterAsync(
            IChannel channel,
            Transport transport,
            List<FiscalPrinterDriver> drivers)
        {
            IFiscalPrinter? printer = null;
            var unknownDeviceConnectedToChannel = true;
            var positionInList = 1;
            var detectionTask = Task.Run(async () =>
            {
                foreach (var driver in drivers)
                {
                    try
                    {
                        var uri = $"{driver.DriverName}.{transport.TransportName}://{channel.Descriptor}";
                        Log.Information($"Probing ({positionInList++}/{drivers.Count}){uri}...");

                        printer = await Task<IFiscalPrinter>.Run(() => driver.Connect(channel, ServiceOptions));
                        printer.DeviceInfo.Uri = uri;

                        // We found our driver, so do not test more
                        unknownDeviceConnectedToChannel = false;
                        Log.Information($"Successfully detected {uri}.");
                        break;
                    }
                    catch (TimeoutException ex)
                    {
                        // Timeout occured while connecting. Skip this transport address.
                        Log.Error($"Timeout occured: {ex.Message}");
                    }
                    catch (InvalidResponseException ex)
                    {
                        // If speed not matched, then skip this transport address detection.
                        if (ex.Message.Contains("communication speed not match"))
                            break;
                        // Autodetect probe not passed for this channel. No response.
                        Log.Information($"Device at {channel.Descriptor} incompatible with protocol {driver.DriverName}: {ex.Message}");
                    }
                    catch (InvalidDeviceInfoException ex)
                    {
                        // Autodetect probe not passed for this channel. Invalid device.
                        Log.Information($"Device at {channel.Descriptor} incompatible with protocol {driver.DriverName}: invalid device info returned ({ex.Message}).");
                    }
                    catch (FileNotFoundException)
                    {
                        // Port not exists
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Unexpected error: {ex.Message}");
                    }
                }
            });

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(ServiceOptions.DetectionTimeout));

            var completedTask = await Task.WhenAny(detectionTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                Log.Error($"Detection for channel {channel.Descriptor} timed out after {ServiceOptions.DetectionTimeout} seconds.");
                return null;
            }

            if (unknownDeviceConnectedToChannel)
            {
                // We did not recognize the device, so drop that channel 
                // and leave it available for others
                // transport.Drop(channel);   // hangs here if device driver problem occure
            }
            Log.Information($"Probing finished for {channel.Descriptor}.");
            return printer;
        }


        /// <summary>
        /// Returns the available fiscal printers.
        /// </summary>
        /// <returns>The available fiscal printers.</returns>
        public IDictionary<string, IFiscalPrinter> DetectAvailablePrinters(IEnumerable<string> excludedPorts)
        {
            var fp = new Dictionary<string, IFiscalPrinter>();
            var transportDrivers = new Dictionary<Transport, List<FiscalPrinterDriver>>();

            foreach (var (driver, transport) in protocols.Values)
            {
                if (!transportDrivers.ContainsKey(transport))
                {
                    transportDrivers[transport] = new List<FiscalPrinterDriver>();
                }
                transportDrivers[transport].Add(driver);
            }

            List<Task<IFiscalPrinter?>> listOfTasks = new List<Task<IFiscalPrinter?>>();
            foreach (KeyValuePair<Transport, List<FiscalPrinterDriver>> td in transportDrivers)
            {
                var transport = td.Key;
                var drivers = td.Value;
                foreach (var (address, _) in transport.GetAvailableAddresses())
                {
                    if ( excludedPorts.ToList().Contains(address))
                    {
                        Log.Information($"Port {address} is excluded from probing.");
                        continue;
                    }
                    try
                    {
                        var channel = transport.OpenChannel(address);
                        listOfTasks.Add(DetectPrinterAsync(channel, transport, drivers));
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Cannot open channel {address}: {ex.Message}");
                    }
                }
            }

            try
            {
                var task = (Task.WhenAll<IFiscalPrinter?>(listOfTasks));
                task.Wait();
                foreach (var printer in task.Result)
                {
                    if (printer != null)
                    {
                        fp.Add(printer.DeviceInfo.Uri, printer);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Probing error: {ex.Message}");
            }
            return fp;
        }

        private static readonly Regex uriPattern = new Regex(
            @"^(?<protocol>.+)://(?<address>.+)$");

        /// <summary>
        /// <para>
        /// Connects to the fiscal printer with the specified device URI.
        /// 
        /// The Uri is in format "protocol://address". For example:
        /// </para>
        /// <para>
        /// bg.dy.json.http://printeraddress
        /// </para>
        /// </summary>
        /// <param name="deviceUri">The URI of the fiscal printer device.</param>
        /// <param name="autoDetect">While parsing the raw device info, driver tries to autodetect the protocol compliance.</param>
        /// <param name="options">Options for the printer.</param>
        /// <returns>
        /// The fiscal printer object.
        /// </returns>
        /// <exception cref="InvalidOperationException">When the printer is not found or the URI is not correctly formatted.</exception>
        public IFiscalPrinter Connect(string deviceUri, ServiceOptions serviceOptions, bool autoDetect = true, IDictionary<string, string>? options = null)
        {
            var match = uriPattern.Match(deviceUri);
            if (!match.Success)
                throw new FormatException(
                    $"'{deviceUri}' is not recognized as valid device URI (protocol://address)");

            var protocol = match.Groups["protocol"].Value;
            var address = match.Groups["address"].Value;

            FiscalPrinterDriver driver;
            Transport transport;
            try
            {
                (driver, transport) = protocols[protocol];
            }
            catch
            {
                throw new InvalidOperationException($"Unknown protocol '{protocol}'.");
            }

            var channel = transport.OpenChannel(address);
            var uri = string.Format($"{driver.DriverName}.{transport.TransportName}://{channel.Descriptor}");
            try
            {
                var p = driver.Connect(channel, serviceOptions, autoDetect, options);
                p.DeviceInfo.Uri = uri;
                return p;
            }
            catch (Exception ex)
            {
                Log.Error($"Cannot connect to {uri}: {ex.Message}");
                throw;
            }
        }

        private readonly Dictionary<string, (FiscalPrinterDriver driver, Transport transport)> protocols =
            new Dictionary<string, (FiscalPrinterDriver driver, Transport transport)>();

        public ServiceOptions ServiceOptions { get; }
    }
}
