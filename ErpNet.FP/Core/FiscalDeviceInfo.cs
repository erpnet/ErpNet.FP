namespace ErpNet.FP.Core
{
    /// <summary>
    /// Provides information about a fiscal device so that unique sales number can be built
    /// </summary>
    public struct FiscalDeviceInfo
    {
        /// <summary>
        /// Version of the device, according to <see cref="Model"/>
        /// </summary>
        public string Version;
        /// <summary>
        /// Model of the device
        /// </summary>
        public string Model;
    }
}
