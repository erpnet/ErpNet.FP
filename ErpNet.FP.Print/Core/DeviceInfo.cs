namespace ErpNet.FP.Print.Core
{
    public class DeviceInfo
    {
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
        /// Optional. 
        /// </summary>
        public string Type;
        /// <summary>
        /// Optional. Firmware version.
        /// </summary>
        public string FirmwareVersion;
    }
}