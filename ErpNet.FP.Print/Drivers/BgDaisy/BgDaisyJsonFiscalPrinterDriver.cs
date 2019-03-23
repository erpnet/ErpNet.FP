using System;
using System.Collections.Generic;
using ErpNet.FP.Print.Core;

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
