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
        /// The receipt amount.
        /// </summary>
        public decimal ReceiptAmount = 0m;
        /// <summary>
        /// The fiscal memory number.
        /// </summary>
        public string FiscalMemorySerialNumber = string.Empty;

        /// <summary>
        /// Wraps <see cref="ReceiptInfo"/> into the flat device-status response returned to callers.
        /// </summary>
        public virtual DeviceStatusWithReceiptInfo ToDeviceStatus(DeviceStatus status)
            => new DeviceStatusWithReceiptInfo(status, this);
    }
}
