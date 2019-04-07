namespace ErpNet.FP.Core
{
    public class DeviceInfo
    {
        /// <summary>
        /// Fiscal printer serial number
        /// </summary>
        public string SerialNumber = string.Empty;
        /// <summary>
        /// Fiscal printer memory serial number
        /// </summary>
        public string FiscalMemorySerialNumber = string.Empty;
        /// <summary>
        /// Company that produces the printer
        /// </summary>
        public string Company = string.Empty;
        /// <summary>
        /// Model
        /// </summary>
        public string Model = string.Empty;
        /// <summary>
        /// Optional. Firmware version.
        /// </summary>
        public string FirmwareVersion = string.Empty;
        // <summary>
        /// Maximum symbols for operator names, item names, department names allowed.
        /// </summary>
        public int ItemTextMaxLength;
        /// <summary>
        /// Maximum symbols for payment names allowed.
        /// </summary>
        public int CommentTextMaxLength;
        /// <summary>
        /// Maximal operator password length allowed;
        /// </summary>
        public int OperatorPasswordMaxLength;
    }
}