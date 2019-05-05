namespace ErpNet.FP.Core
{
    /// <summary>
    /// Information returned after printing receipt.
    /// </summary>
    public class ReceiptInfo
    {
        /// <summary>
        /// The receipt number.
        /// </summary>
        public string ReceiptNumber = string.Empty;
        /// <summary>
        /// The receipt date and time.
        /// </summary>
        public System.DateTime ReceiptDateTime;
        /// <summary>
        /// The device fiscal memory serial number.
        /// </summary>
        public string FiscalMemorySerialNumber = string.Empty;
        /// <summary>
        /// The receipt amount.
        /// </summary>
        public decimal ReceiptAmount = 0m;
    }
}
