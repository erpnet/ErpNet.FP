namespace ErpNet.FP.Core
{
    /// <summary>
    /// Represents the capabilities of a connected fiscal printer.
    /// </summary>
    public interface IFiscalPrinter
    {
        /// <summary>
        /// Gets information about the connected device.
        /// </summary>
        /// <returns>Device information.</returns>
        DeviceInfo DeviceInfo { get; }

        /// <summary>
        /// Checks whether the device is currently ready to accept commands.
        /// </summary>
        DeviceStatus CheckStatus();

        /// <summary>
        /// Prints the specified receipt.
        /// </summary>
        /// <param name="receipt">The receipt to print.</param>
        (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt);

        /// <summary>
        /// Prints the specified reversal receipt.
        /// </summary>
        /// <param name="reversalReceipt">The reversal receipt.</param>
        /// <returns></returns>
        DeviceStatus PrintReversalReceipt(ReversalReceipt reversalReceipt);

        /// <summary>
        /// Prints a deposit money note.
        /// </summary>
        /// <param name="amount">The deposited amount. Should be greater than 0.</param>
        DeviceStatus PrintMoneyDeposit(decimal amount);

        /// <summary>
        /// Prints a withdraw money note.
        /// </summary>
        /// <param name="amount">The withdrawn amount. Should be greater than 0.</param>
        DeviceStatus PrintMoneyWithdraw(decimal amount);

        /// <summary>
        /// Prints a zeroing report.
        /// </summary>
        DeviceStatus PrintZeroingReport();
    }
}