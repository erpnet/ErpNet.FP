using ErpNet.Fiscal.Print.Core;
using System.Collections.Generic;

namespace ErpNet.Fiscal.Print.Drivers.BgDaisy
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Daisy Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.Fiscal.Drivers.BgIslFiscalPrinter" />
    public class BgDaisyIslFiscalPrinter : BgIslFiscalPrinter
    {
        protected const byte
            DaisyCommandGetDeviceConstants = 0x80,
            DaisyCommandAbortFiscalReceipt = 0x82;

        public BgDaisyIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options)
        {
        }

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
