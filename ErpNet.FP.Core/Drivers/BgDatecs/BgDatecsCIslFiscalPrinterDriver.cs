using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    public class BgDatecsCIslFiscalPrinterDriver : FiscalPrinterDriver
    {
        protected readonly string SerialNumberPrefix = "DT";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.c.isl";

        public override IFiscalPrinter Connect(IChannel channel, IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgDatecsCIslFiscalPrinter(channel, options);
            var (rawDeviceInfo, _) = fiscalPrinter.GetRawDeviceInfo();
            fiscalPrinter.Info = ParseDeviceInfo(rawDeviceInfo);
            var (TaxIdentificationNumber, _) = fiscalPrinter.GetTaxIdentificationNumber();
            fiscalPrinter.Info.TaxIdentificationNumber = TaxIdentificationNumber;
            return fiscalPrinter;
        }

        protected DeviceInfo ParseDeviceInfo(string rawDeviceInfo)
        {
            var commaFields = rawDeviceInfo.Split(',');
            if (commaFields.Length != 6)
            {
                throw new InvalidDeviceInfoException($"rawDeviceInfo must contain 6 comma-separated items for '{DriverName}'");
            }
            var serialNumber = commaFields[4];
            if (serialNumber.Length != 8 || !serialNumber.StartsWith(SerialNumberPrefix))
            {
                throw new InvalidDeviceInfoException($"serial number must begin with {SerialNumberPrefix} and be with length 8 characters for '{DriverName}'");
            }
            var modelName = commaFields[0];
            if (modelName.EndsWith("X") || (
                !modelName.StartsWith("DP") &&
                !modelName.StartsWith("WP")))
            {
                throw new InvalidDeviceInfoException($"incompatible with '{DriverName}'");
            }
            var info = new DeviceInfo
            {
                SerialNumber = serialNumber,
                FiscalMemorySerialNumber = commaFields[5],
                Model = modelName,
                FirmwareVersion = commaFields[1],
                Manufacturer = "Datecs",
                CommentTextMaxLength = 42, // Set by Datecs protocol
                ItemTextMaxLength = 22, // Set by Datecs protocol
                OperatorPasswordMaxLength = 8 // Set by Datecs protocol
            };
            return info;
        }
    }
}
