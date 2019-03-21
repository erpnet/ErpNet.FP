using System;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Print.Drivers.BgDaisy
{
    public class BgDaisyJsonHttpFiscalPrinterDriver : FiscalPrinterDriver
    {
        public override string ProtocolName => "bg.dy.json.http";

        public override IFiscalPrinter Connect(string address)
        {
            throw new NotImplementedException();
            //return new BgDaisyJsonHttpFiscalPrinter(address);
        }

        public override DeviceInfo DetectLocalFiscalPrinter(string portName)
        {
            // This is remote printer driver. Detection is not possible.
            return null;
        }
    }
}
