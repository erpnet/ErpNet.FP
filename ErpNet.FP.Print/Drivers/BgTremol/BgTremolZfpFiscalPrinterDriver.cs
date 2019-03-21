using System;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Print.Drivers.BgTremol
{
    public class BgTremolZfpFiscalPrinterDriver : FiscalPrinterDriver
    {
        public override string ProtocolName => "bg.tr.zfp";

        public override IFiscalPrinter Connect(string address)
        {
            throw new NotImplementedException();
        }

        public override DeviceInfo DetectLocalFiscalPrinter(string portName)
        {
            throw new NotImplementedException();
        }
    }
}
