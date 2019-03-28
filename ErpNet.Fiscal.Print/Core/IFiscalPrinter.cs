namespace ErpNet.Fiscal.Print.Core
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
        /// <returns>
        ///   <c>true</c> if the device is ready; otherwise, <c>false</c>.
        /// </returns>
        bool IsReady();

        /// <summary>
        /// Prints the specified receipt.
        /// </summary>
        /// <param name="receipt">The receipt to print.</param>
        PrintInfo PrintReceipt(Receipt receipt);

        /// <summary>
        /// Prints the specified reversal receipt.
        /// </summary>
        /// <param name="reversalReceipt">The reversal receipt.</param>
        /// <returns></returns>
        PrintInfo PrintReversalReceipt(Receipt reversalReceipt);

        /// <summary>
        /// Prints a deposit money note.
        /// </summary>
        /// <param name="amount">The deposited amount. Should be greater than 0.</param>
        PrintInfo PrintMoneyDeposit(decimal amount);

        /// <summary>
        /// Prints a withdraw money note.
        /// </summary>
        /// <param name="amount">The withdrawn amount. Should be greater than 0.</param>
        PrintInfo PrintMoneyWithdraw(decimal amount);

        /// <summary>
        /// Prints a zeroing report.
        /// </summary>
        PrintInfo PrintZeroingReport();
    }
}