using ErpNet.FP.Print.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Print.Drivers.BgEltrade
{
    public class BgEltradeIslFiscalPrinterDriver : FiscalPrinterDriver
    {
        public override string DriverName => "bg.ed.isl";

        public override IFiscalPrinter Connect(IChannel channel, IDictionary<string, string> options = null)
        {
            var fiscalPrinter = new BgEltradeIslFiscalPrinter(channel, options);
            fiscalPrinter.FiscalPrinterInfo = ParseDeviceInfo(fiscalPrinter.ReadRawDeviceInfo());
            return fiscalPrinter;
        }

        protected DeviceInfo ParseDeviceInfo(string rawDeviceInfo)
        {
            var commaFields = rawDeviceInfo.Split(',');            
            if (commaFields.Length != 7) {
                throw new InvalidDeviceInfoException("rawDeviceInfo must contain 7 comma-separated items");
            }
            var serialNumber = commaFields[5];
            if (serialNumber.Length != 8 || !serialNumber.StartsWith("ED")) {
                throw new InvalidDeviceInfoException("serial number must begin with DY and be with length 8 characted");
            }
            var info = new DeviceInfo();
            info.SerialNumber = serialNumber;
            info.FiscalMemorySerialNumber = commaFields[6];
            info.Model = commaFields[0];
            info.FirmwareVersion = commaFields[2];
            info.Company = "Eltrade";
            return info;
        }
    }
}
