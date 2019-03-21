using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ErpNet.FP.Print.Core;
using ErpNet.FP.Print.Drivers.BgDaisy;
using ErpNet.FP.Print.Drivers.BgTremol;

namespace ErpNet.FP.Print.Provider
{
    /// <summary>
    /// General functions for finding and connecting fiscal printers.
    /// </summary>
    public static class Provider
    {
        private static List<FiscalPrinterDriver> drivers = null;

        /// <summary>
        /// Gets all printer drivers.
        /// </summary>
        /// <returns>All printer drivers.</returns>
        public static IEnumerable<FiscalPrinterDriver> GetDrivers()
        {
            if (drivers == null)
            {
                lock (drivers)
                {
                    drivers = new List<FiscalPrinterDriver>();
                    drivers.Add(new BgDaisyIslFiscalPrinterDriver());
                    drivers.Add(new BgDaisyJsonHttpFiscalPrinterDriver());
                    drivers.Add(new BgTremolZfpFiscalPrinterDriver());
                }
            }
            return drivers;
        }

        /// <summary>
        /// Gets the local ports.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetLocalPorts()
        {
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Returns the URIs (as keys) and device information (as values) of the detected locally connected fiscal printers.
        /// </summary>
        /// <returns>The URIs of the locally connected fiscal printers.</returns>
        public static IEnumerable<DeviceInfo> DetectLocalDevices()
        {
            foreach (var port in GetLocalPorts())
            {
                foreach (var driver in GetDrivers())
                {
                    var di = driver.DetectLocalFiscalPrinter(port);
                    if (di != null)
                        yield return di;
                }
            }
        }

        private static readonly Regex protocolRegex = new Regex(
            @"^(?<protocol>.+)://(?<address>.+)$",
            RegexOptions.Compiled
        );

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
        public static IFiscalPrinter Connect(string deviceUri)
        {
            string protocol = null; //get bg.dy.json.http or similar protocol
            string address = null; //get the address part

            var match = protocolRegex.Match(deviceUri);
            if (!match.Success)
            {
                throw new FormatException($"The value of {nameof(deviceUri)}, {deviceUri}, cannot be parsed as as protocol://address");
            }

            protocol = match.Groups["protocol"].Value;
            address = match.Groups["addresss"].Value;

            foreach (var driver in GetDrivers())
            {
                if (driver.ProtocolName == protocol)
                    return driver.Connect(address);
            }
            throw new InvalidOperationException($"Protocol '{protocol}' not recognized.");
        }


    }
}
