using System;
using System.Collections.Generic;

namespace ErpNet.FP.Print.Core
{
    /// <summary>
    /// Represents a driver for a fiscal printer
    /// </summary>
    public abstract class FiscalPrinterDriver
    {
        /// <summary>
        /// Gets the name of the printer driver.
        /// Example: "bg.dy.json".
        /// </summary>
        /// <value>
        /// The name of the printer driver.
        /// </value>
        public abstract string DriverName { get; }

        /// <summary>
        /// Returns a new fiscal printer, connected to the specified <see cref="IChannel" />.
        /// Throws exception if the connection is not successful.
        /// </summary>
        /// <param name="channel">The transport channel, which should be used to connect the printer.</param>
        /// <param name="options">The options to pass to the driver.</param>
        /// <returns>
        /// New fiscal printer instance.
        /// </returns>
        public abstract IFiscalPrinter Connect(
            IChannel channel,
            IDictionary<string, string> options = null);
    }
}
