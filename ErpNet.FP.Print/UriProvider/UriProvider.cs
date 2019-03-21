using System;
using System.Collections.Generic;

namespace ErpNet.FP.Print.UriProvider
{
    /// <summary>
    /// General functions for finding and connecting fiscal printers.
    /// </summary>
    public static class UriProvider
    {
        /// <summary>
        /// Returns the URIs (as keys) and device information (as values) of the detected locally connected fiscal printers.
        /// </summary>
        /// <returns>The URIs of the locally connected fiscal printers.</returns>
        public static Dictionary<string, DeviceInfo> DetectLocalDevices() { return null; }

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
        public static IFiscalPrinter Connect(string deviceUri, PrintOptions options)
        {
            string protocol = null; //get bg.dy.json.http or similar protocol
            string address = null; //get the address part

            switch (protocol)
            {
                case "bg.dy.json.http":
                    return new Drivers.BgDaisy.BgDaisyJsonHttpFiscalPrinter(address, options);
                case "bg.tr.zfp.http":
                    return new Drivers.BgTremol.BgTremolZfpHttpFiscalPrinter(address, options);
                default:
                    throw new InvalidOperationException($"Protocol '{protocol}' not recognized.");
            }
        }


    }
}
