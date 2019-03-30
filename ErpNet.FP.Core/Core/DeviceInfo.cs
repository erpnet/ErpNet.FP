namespace ErpNet.FP.Core
{
    public class DeviceInfo
    {
        /// <summary>
        /// The address part of the fiscal printer Uri.
        /// </summary>
        public string Address;
        /// <summary>
        /// Fiscal printer serial number
        /// </summary>
        public string SerialNumber;
        /// <summary>
        /// Fiscal printer memory serial number
        /// </summary>
        public string FiscalMemorySerialNumber;
        /// <summary>
        /// Company that produces the printer
        /// </summary>
        public string Company;
        /// <summary>
        /// Model
        /// </summary>
        public string Model;
        /// <summary>
        /// Optional. Type of the device.
        /// </summary>
        public string Type;
        /// <summary>
        /// Optional. Firmware version.
        /// </summary>
        public string FirmwareVersion;
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