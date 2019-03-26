using ErpNet.FP.Print.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Print.Drivers.BgDaisy
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Daisy Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public class BgDaisyIslFiscalPrinter : BgIslFiscalPrinter
    {
        protected const byte
            DaisyCommandGetDeviceConstants = 0x80;

        public BgDaisyIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options)
        {
        }

        protected override DeviceStatus ParseStatus(byte[] status)
        {
            // TODO: Device status parser
            return new DeviceStatus();
        }

        public (string, DeviceStatus) GetRawDeviceConstants()

        {
            return Request(DaisyCommandGetDeviceConstants);
        }

    }
}
