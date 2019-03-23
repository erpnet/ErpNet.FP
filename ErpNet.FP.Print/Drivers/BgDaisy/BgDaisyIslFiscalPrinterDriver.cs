using System;
using System.Collections.Generic;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Print.Drivers.BgDaisy
{
    public class BgDaisyIslFiscalPrinterDriver : FiscalPrinterDriver
    {
        public override string DriverName => "bg.dy.isl";

        public override IFiscalPrinter Connect(IChannel channel, IDictionary<string, string> options = null)
        {
            throw new NotImplementedException();
        }
    }
}
