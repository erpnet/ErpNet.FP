namespace ErpNet.FP.Print.Core
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
    }
}