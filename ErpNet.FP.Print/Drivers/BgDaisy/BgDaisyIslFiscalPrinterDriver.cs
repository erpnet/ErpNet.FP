using ErpNet.FP.Print.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Print.Drivers.BgDaisy
{
    public class BgDaisyIslFiscalPrinterDriver : FiscalPrinterDriver
    {
        public override string DriverName => "bg.dy.isl";

        public override IFiscalPrinter Connect(IChannel channel, IDictionary<string, string> options = null)
        {
            var fiscalPrinter = new BgDaisyIslFiscalPrinter(channel, options);
            fiscalPrinter.FiscalPrinterInfo = ParseDeviceInfo(fiscalPrinter.ReadRawDeviceInfo());
            return fiscalPrinter;
        }

        protected DeviceInfo ParseDeviceInfo(string rawDeviceInfo)
        {
            var commaFields = rawDeviceInfo.Split(',');            
            if (commaFields.Length != 6) {
                throw new InvalidDeviceInfoException("rawDeviceInfo must contain 6 comma-separated items");
            }
            var serialNumber = commaFields[4];
            if (serialNumber.Length != 8 || !serialNumber.StartsWith("DY")) {
                throw new InvalidDeviceInfoException("serial number must begin with DY and be with length 8 characted");
            }
            var spaceFields = commaFields[0].Split(' ');
            if (spaceFields.Length != 4) {
                throw new InvalidDeviceInfoException("first member of comma separated list must contain 4 whitespace-separated values");
            }
            var info = new DeviceInfo();
            info.SerialNumber = serialNumber;
            info.FiscalMemorySerialNumber = commaFields[5];
            info.Model = spaceFields[0];
            info.FirmwareVersion = spaceFields[1];
            info.Company = "Daisy";
            return info;
        }
    }
}
