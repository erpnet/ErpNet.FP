namespace ErpNet.FP.Print.Core
{
    /// <summary>
    /// Represents a driver for a fiscal printer
    /// </summary>
    public abstract class FiscalPrinterDriver
    {
        /// <summary>
        /// Gets the name of the protocol, used when creating device Uri.
        /// Example: "bg.dy.json.http".
        /// </summary>
        /// <value>
        /// The name of the protocol.
        /// </value>
        public abstract string ProtocolName { get; }

        /// <summary>
        /// Detects if there is compatible fiscal printer on the specified local COM port.
        /// </summary>
        /// <param name="portName">Name of the local COM port.</param>
        /// <returns>Device information if there is compatible fiscal printer.</returns>
        public abstract DeviceInfo DetectLocalFiscalPrinter(string portName);

        /// <summary>
        /// Returns a new fiscal printer, connected to the specified address.
        /// Throws error if not successful.
        /// </summary>
        /// <param name="address">The address part of the device Uri (without the protocol part).</param>
        /// <returns>New fiscal printer if the operation is successful. Throws error if not successful.</returns>
        public abstract IFiscalPrinter Connect(string address);
    }
}
