using System;
using System.Collections.Generic;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Print.Drivers.BgDatecs
{
    public class BgDatecsIslFiscalPrinterDriver : FiscalPrinterDriver
    {
        public override string DriverName => "bg.dt.isl";

        public override IFiscalPrinter Connect(IChannel channel, IDictionary<string, string> options = null) {
            var fiscalPrinter = new BgDatecsIslFiscalPrinter(channel, options);
            fiscalPrinter.FiscalPrinterInfo = ParseDeviceInfo(fiscalPrinter.ReadRawDeviceInfo());
            return fiscalPrinter;
        }

        protected DeviceInfo ParseDeviceInfo(string rawDeviceInfo) {
            var commaFields = rawDeviceInfo.Split(',');            
            if (commaFields.Length != 6) {
                throw new InvalidDeviceInfoException("rawDeviceInfo must contain 6 comma-separated items");
            }
            var serialNumber = commaFields[4];
            if (serialNumber.Length != 8 || !serialNumber.StartsWith("DT")) {
                throw new InvalidDeviceInfoException("serial number must begin with DT and be with length 8 characters");
            }
            var info = new DeviceInfo();
            info.SerialNumber = serialNumber;
            info.FiscalMemorySerialNumber = commaFields[5];
            info.Model = commaFields[0];
            info.FirmwareVersion = commaFields[1];
            info.Company = "Datecs";
            return info;
        }
    }
}
