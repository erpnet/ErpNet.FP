using System;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Print.Drivers.BgDaisy
{
    public class BgDaisyIslFiscalPrinterDriver : FiscalPrinterDriver
    {
        public override string ProtocolName => "bg.dy.isl";

        public override IFiscalPrinter Connect(string address)
        {
            //return new BgDaisyIslFiscalPrinter(address);
            throw new NotImplementedException();
        }

        public override DeviceInfo DetectLocalFiscalPrinter(string portName)
        {
            throw new NotImplementedException();
        }
    }
}
