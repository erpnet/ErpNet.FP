using ErpNet.Fiscal.Print.Core;
using System.Collections.Generic;

namespace ErpNet.Fiscal.Print.Drivers.BgDaisy
{
    public class BgDaisyJsonFiscalPrinterDriver : FiscalPrinterDriver
    {
        public override string SerialNumberPrefix => "DY";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.json";

        public override IFiscalPrinter Connect(
            IChannel channel,
            IDictionary<string, string> options = null)
        {
            return new BgDaisyJsonFiscalPrinter(channel, options);
        }
    }
}
