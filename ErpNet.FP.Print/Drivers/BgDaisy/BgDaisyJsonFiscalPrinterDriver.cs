using ErpNet.FP.Print.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Print.Drivers.BgDaisy
{
    public class BgDaisyJsonFiscalPrinterDriver : FiscalPrinterDriver
    {
        public override string DriverName => "bg.dy.json";

        public override IFiscalPrinter Connect(
            IChannel channel, 
            IDictionary<string, string> options = null)
        {
            return new BgDaisyJsonFiscalPrinter(channel, options);
        }
    }
}
