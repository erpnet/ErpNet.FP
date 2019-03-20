using System;
using ErpNet.FP.Core;

namespace ErpNet.FP
{
    /// <summary>
    /// General functions for finding and connecting fiscal printers.
    /// </summary>
    public static class Provider
    {
        /// <summary>
        /// Returns the URIs of the locally connected recognized fiscal printers.
        /// </summary>
        /// <returns>The URIs of the locally connected fiscal printers.</returns>
        public static string[] GetLocalDevices() { return null; }

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
                    return new Drivers.BgDyJsonHttp(address, options);
                default:
                    throw new InvalidOperationException($"Protocol '{protocol}' not recognized.");
            }
        }


    }
}
