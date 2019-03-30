using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgDaisy
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Daisy Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public class BgDaisyIslFiscalPrinter : BgIslFiscalPrinter
    {
        protected const byte
            DaisyCommandGetDeviceConstants = 0x80,
            DaisyCommandAbortFiscalReceipt = 0x82;

        public BgDaisyIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options) { }

        public override IDictionary<string, string> GetDefaultOptions()
        {
            return new Dictionary<string, string>
            {
                ["Operator.ID"] = "1",
                ["Operator.Password"] = "1",

                ["Administrator.ID"] = "20",
                ["Administrator.Password"] = "9999"
            };
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
