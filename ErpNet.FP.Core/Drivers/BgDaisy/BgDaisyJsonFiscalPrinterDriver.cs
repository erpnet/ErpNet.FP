using ErpNet.FP.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgDaisy
{
    public class BgDaisyJsonFiscalPrinterDriver : FiscalPrinterDriver
    {
        protected readonly string SerialNumberPrefix = "DY";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.json";

        public override IFiscalPrinter Connect(
            IChannel channel,
            IDictionary<string, string> options = null)
        {
            return new BgDaisyJsonFiscalPrinter(channel, options);
        }
    }
}
