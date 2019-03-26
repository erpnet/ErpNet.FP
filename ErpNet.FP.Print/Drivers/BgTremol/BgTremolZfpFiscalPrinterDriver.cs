using System;
using System.Collections.Generic;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Print.Drivers.BgTremol
{
    public class BgTremolZfpFiscalPrinterDriver : FiscalPrinterDriver
    {

        public override string SerialNumberPrefix => "ED";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.zfp";

        public override IFiscalPrinter Connect(IChannel channel, IDictionary<string, string> options = null)
        => new BgTremolZfpFiscalPrinter(channel, options);
    }
}
