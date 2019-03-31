using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgDaisy
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Daisy Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgDaisyIslFiscalPrinter : BgIslFiscalPrinter
    {
        protected const byte
            DaisyCommandGetDeviceConstants = 0x80,
            DaisyCommandAbortFiscalReceipt = 0x82;

        protected override DeviceStatus ParseStatus(byte[] status)
        {
            // TODO: Device status parser
            return new DeviceStatus();
        }

        public override (string, DeviceStatus) AbortReceipt()
        {
            return Request(DaisyCommandAbortFiscalReceipt);
        }

        public (string, DeviceStatus) GetRawDeviceConstants()

        {
            return Request(DaisyCommandGetDeviceConstants);
        }

    }
}
