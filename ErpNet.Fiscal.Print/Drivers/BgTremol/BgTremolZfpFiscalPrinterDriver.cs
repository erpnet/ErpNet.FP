using System;
using System.Collections.Generic;
using ErpNet.Fiscal.Print.Core;

namespace ErpNet.Fiscal.Print.Drivers.BgTremol
{
    public class BgTremolZfpFiscalPrinterDriver : FiscalPrinterDriver
    {

        public override string SerialNumberPrefix => "TR";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.zfp";

        public override IFiscalPrinter Connect(IChannel channel, IDictionary<string, string> options = null)
        {
            var fiscalPrinter = new BgTremolZfpFiscalPrinter(channel, options);
            var (rawDeviceInfo, _) = fiscalPrinter.GetRawDeviceInfo();
            //fiscalPrinter.Info = ParseDeviceInfo(rawDeviceInfo);
            return fiscalPrinter;
        }
    }
}
