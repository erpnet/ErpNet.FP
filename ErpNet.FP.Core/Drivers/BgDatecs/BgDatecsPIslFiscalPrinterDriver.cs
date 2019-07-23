using System.Collections.Generic;

#nullable enable
namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    public class BgDatecsPIslFiscalPrinterDriver : FiscalPrinterDriver
    {
        protected readonly string SerialNumberPrefix = "DT";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.p.isl";

        public override IFiscalPrinter Connect(IChannel channel, bool autoDetect = true, IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgDatecsPIslFiscalPrinter(channel, options);
            var (rawDeviceInfo, _) = fiscalPrinter.GetRawDeviceInfo();
            fiscalPrinter.Info = ParseDeviceInfo(rawDeviceInfo, autoDetect);
            var (TaxIdentificationNumber, _) = fiscalPrinter.GetTaxIdentificationNumber();
            fiscalPrinter.Info.TaxIdentificationNumber = TaxIdentificationNumber;
            return fiscalPrinter;
        }

        protected DeviceInfo ParseDeviceInfo(string rawDeviceInfo, bool autoDetect)
        {
            var commaFields = rawDeviceInfo.Split(',');
            if (commaFields.Length != 6)
            {
                throw new InvalidDeviceInfoException($"rawDeviceInfo must contain 6 comma-separated items for '{DriverName}'");
            }
            var serialNumber = commaFields[4];
            var modelName = commaFields[0];
            if (autoDetect)
            {
                if (serialNumber.Length != 8 || !serialNumber.StartsWith(SerialNumberPrefix, System.StringComparison.Ordinal))
                {
                    throw new InvalidDeviceInfoException($"serial number must begin with {SerialNumberPrefix} and be with length 8 characters for '{DriverName}'");
                }

                if (modelName.EndsWith("X", System.StringComparison.Ordinal) || (
                    !modelName.StartsWith("FP", System.StringComparison.Ordinal) &&
                    !modelName.StartsWith("FMP", System.StringComparison.Ordinal) &&
                    !modelName.StartsWith("SK", System.StringComparison.Ordinal)))
                {
                    throw new InvalidDeviceInfoException($"incompatible with '{DriverName}'");
                }
            }
            var info = new DeviceInfo
            {
                SerialNumber = serialNumber,
                FiscalMemorySerialNumber = commaFields[5],
                Model = modelName,
                FirmwareVersion = commaFields[1],
                Manufacturer = "Datecs",
                CommentTextMaxLength = 42, // Set by Datecs protocol
                ItemTextMaxLength = 36, // Set by Datecs protocol
                OperatorPasswordMaxLength = 8 // Set by Datecs protocol
            };
            return info;
        }
    }
}
