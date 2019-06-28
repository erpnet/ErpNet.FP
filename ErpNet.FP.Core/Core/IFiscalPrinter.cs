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
        DeviceStatusWithDateTime CheckStatus();

        /// <summary>
        /// Gets the amount of cash available
        /// </summary>
        DeviceStatusWithCashAmount Cash();

        /// <summary>
        /// Sets the device date and time
        /// </summary>
        DeviceStatus SetDateTime(CurrentDateTime currentDateTime);

        /// <summary>
        /// Prints the specified receipt.
        /// </summary>
        /// <param name="receipt">The receipt to print.</param>
        (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt);

        /// <summary>
        /// Validates the receipt object
        /// </summary>
        /// <param name="receipt"></param>
        /// <returns></returns>
        DeviceStatus ValidateReceipt(Receipt receipt);

        /// <summary>
        /// Prints the specified reversal receipt.
        /// </summary>
        /// <param name="reversalReceipt">The reversal receipt.</param>
        /// <returns></returns>
        DeviceStatus PrintReversalReceipt(ReversalReceipt reversalReceipt);

        /// <summary>
        /// Validates the reversal receipt object
        /// </summary>
        /// <param name="reversalReceipt"></param>
        /// <returns></returns>
        DeviceStatus ValidateReversalReceipt(ReversalReceipt reversalReceipt);

        /// <summary>
        /// Prints a deposit money note.
        /// </summary>
        /// <param name="amount">The deposited amount. Should be greater than 0.</param>
        DeviceStatus PrintMoneyDeposit(TransferAmount transferAmount);

        /// <summary>
        /// Prints a withdraw money note.
        /// </summary>
        /// <param name="amount">The withdrawn amount. Should be greater than 0.</param>
        DeviceStatus PrintMoneyWithdraw(TransferAmount transferAmount);

        /// <summary>
        /// Validates transfer amount object
        /// </summary>
        /// <param name="transferAmount"></param>
        /// <returns></returns>
        DeviceStatus ValidateTransferAmount(TransferAmount transferAmount);

        /// <summary>
        /// Prints a zreport.
        /// </summary>
        DeviceStatus PrintZReport(Credentials credentials);

        /// <summary>
        /// Prints a xreport.
        /// </summary>
        DeviceStatus PrintXReport(Credentials credentials);
    }
}