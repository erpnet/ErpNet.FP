using ErpNet.FP.Core.Drivers;
using ErpNet.FP.Core.Logging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#nullable enable
namespace ErpNet.FP.Core.Provider
{
    /// <summary>
    /// General functions for finding and connecting fiscal printers.
    /// </summary>
    public class Provider
    {
        private readonly Dictionary<string, (FiscalPrinterDriver driver, Transport transport)> protocols =
            new Dictionary<string, (FiscalPrinterDriver driver, Transport transport)>();

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

        /// <summary>
        /// Returns the available fiscal printers.
        /// </summary>
        /// <returns>The available fiscal printers.</returns>
        public IDictionary<string, IFiscalPrinter> DetectAvailablePrinters()
        {
            // This is naive implementation, which serializes the whole detection process.
            // It can be optimized with parallelism. 
            // However, port contention issues must be resolved in a more elaborate implementation.

            var fp = new Dictionary<string, IFiscalPrinter>();
            var transportDrivers = new Dictionary<Transport, List<FiscalPrinterDriver>>();

            foreach (var protocol in protocols.Values)
            {
                if (!transportDrivers.ContainsKey(protocol.transport))
                {
                    transportDrivers[protocol.transport] = new List<FiscalPrinterDriver>();
                }
                transportDrivers[protocol.transport].Add(protocol.driver);
            }
            foreach (KeyValuePair<Transport, List<FiscalPrinterDriver>> td in transportDrivers)
            {
                var transport = td.Key;
                var drivers = td.Value;
                foreach (var availableAddress in transport.GetAvailableAddresses())
                {
                    try
                    {
                        var channel = transport.OpenChannel(availableAddress.address);
                        var unknownDeviceConnectedToChannel = true;
                        foreach (var driver in drivers)
                        {
                            try
                            {
                                Log.Information($"Probing {driver.DriverName}.{transport.TransportName}://{availableAddress.address}... ");
                                var p = driver.Connect(channel);
                                var uri = string.Format($"{driver.DriverName}.{transport.TransportName}://{channel.Descriptor}");
                                p.DeviceInfo.Uri = uri;
                                fp.Add(uri, p);
                                // We found our driver, so do not test more
                                unknownDeviceConnectedToChannel = false;
                                break;
                            }
                            catch(TimeoutException ex)
                            {
                                // Timeout occured while connecting. Skip this transport address.
                                Log.Error($"Timeout occured: {ex.Message}");
                            }
                            catch (InvalidResponseException ex)
                            {
                                // Autodetect probe not passed for this channel. No response.
                                Log.Information($"Autodetect probe not passed for this channel: {ex.Message}");
                            }
                            catch (InvalidDeviceInfoException ex)
                            {
                                // Autodetect probe not passed for this channel. Invalid device.
                                Log.Information($"Autodetect probe not passed for this channel: {ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Unexpected error: {ex.Message}");
                            }
                        }
                        if (unknownDeviceConnectedToChannel)
                        {
                            // There is an unknown or undetected device connected 
                            // on this channel. So drop the unknown device's channel
                            transport.Drop(channel);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Cannot open channel
                        Log.Error($"Cannot open channel: {ex.Message}");
                    }
                }
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
        public IFiscalPrinter Connect(string deviceUri, bool autoDetect = true, IDictionary<string, string>? options = null)
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
            var p = driver.Connect(channel, autoDetect, options);
            p.DeviceInfo.Uri = uri;
            return p;
        }


    }
}
