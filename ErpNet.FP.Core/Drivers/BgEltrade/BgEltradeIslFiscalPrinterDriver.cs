using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgEltrade
{
    public class BgEltradeIslFiscalPrinterDriver : FiscalPrinterDriver
    {
        protected readonly string SerialNumberPrefix = "ED";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.isl";

        public override IFiscalPrinter Connect(IChannel channel, IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgEltradeIslFiscalPrinter(channel, options);
            var (rawDeviceInfo, _) = fiscalPrinter.GetRawDeviceInfo();
            fiscalPrinter.Info = ParseDeviceInfo(rawDeviceInfo);
            var (TaxIdentificationNumber, _) = fiscalPrinter.GetTaxIdentificationNumber();
            fiscalPrinter.Info.TaxIdentificationNumber = TaxIdentificationNumber;
            return fiscalPrinter;
        }

        protected DeviceInfo ParseDeviceInfo(string rawDeviceInfo)
        {
            var commaFields = rawDeviceInfo.Split(',');
            if (commaFields.Length != 7)
            {
                throw new InvalidDeviceInfoException($"rawDeviceInfo must contain 7 comma-separated items for '{DriverName}'");
            }
            var serialNumber = commaFields[5];
            if (serialNumber.Length != 8 || !serialNumber.StartsWith(SerialNumberPrefix))
            {
                throw new InvalidDeviceInfoException($"serial number must begin with {SerialNumberPrefix} and be with length 8 characters for '{DriverName}'");
            }
            var info = new DeviceInfo
            {
                SerialNumber = serialNumber,
                FiscalMemorySerialNumber = commaFields[6],
                Model = commaFields[0],
                FirmwareVersion = commaFields[2],
                Manifacturer = "Eltrade",
                CommentTextMaxLength = 46, // Set by Eltrade protocol
                ItemTextMaxLength = 30, // Set by Eltrade protocol
                OperatorPasswordMaxLength = 8 // Set by Eltrade protocol
            };
            return info;
        }

    }
}
