using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Print.Provider
{
    /// <summary>
    /// General functions for finding and connecting fiscal printers.
    /// </summary>
    public class Provider
    {
        private Dictionary<string, (FiscalPrinterDriver driver, Transport transport)> protocols =
            new Dictionary<string, (FiscalPrinterDriver driver, Transport transport)>();

        /// <summary>
        /// Adds the specified protocol to the provider.
        /// A protocol consists of a printer driver and a transport.
        /// </summary>
        /// <param name="driver">The driver.</param>
        /// <param name="transport">The transport.</param>
        public void Add(FiscalPrinterDriver driver, Transport transport)
        {
            var key = driver.DriverName + "." + transport.TransportName;
            protocols.Add(key, (driver, transport));
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

            foreach (var (driver, transport) in protocols.Values)
            {
                foreach (var (address, _) in transport.GetAvailableAddresses())
                {
                    try
                    {
                        var channel = transport.OpenChannel(address);
                        try
                        {
                            var p = driver.Connect(channel);
                            fp.Add(string.Format($"{driver.DriverName}.{transport.TransportName}://{channel.Descriptor}"), p);
                        }
                        catch
                        {
                            // Cannot connect to opened channel, possible incompatibility
                        }
                    }
                    catch
                    {
                        // Cannot open channel
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
        /// The Uri is in format "protocol://address". For example:
        /// </para>
        /// <para>
        /// bg.dy.json.http://printeraddress
        /// </para>
        /// </summary>
        /// <param name="deviceUri">The URI of the fiscal printer device.</param>
        /// <param name="options">Options for the printer.</param>
        /// <returns>
        /// The fiscal printer object.
        /// </returns>
        /// <exception cref="InvalidOperationException">When the printer is not found or the URI is not correctly formatted.</exception>
        public IFiscalPrinter Connect(string deviceUri, IDictionary<string, string> options = null)
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
                throw new InvalidOperationException($"Protocol '{protocol}' not recognized.");
            }

            var channel = transport.OpenChannel(address);
            return driver.Connect(channel, options);
        }


    }
}
