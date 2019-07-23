using System.Collections.Generic;

#nullable enable
namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    public class BgDatecsXIslFiscalPrinterDriver : FiscalPrinterDriver
    {
        protected readonly string SerialNumberPrefix = "DT";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.x.isl";

        public override IFiscalPrinter Connect(IChannel channel, bool autoDetect = true, IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgDatecsXIslFiscalPrinter(channel, options);
            var (rawDeviceInfo, _) = fiscalPrinter.GetRawDeviceInfo();
            fiscalPrinter.Info = ParseDeviceInfo(rawDeviceInfo, autoDetect);
            var (TaxIdentificationNumber, _) = fiscalPrinter.GetTaxIdentificationNumber();
            fiscalPrinter.Info.TaxIdentificationNumber = TaxIdentificationNumber;
            return fiscalPrinter;
        }

        protected int getPrintColumnsOfModel(string modelName)
        {
            /*
            PrintColumns - Number of printer columns:
            - for FP-700X = 42, 48 or 64 columns;
            - for FMP-350X = 42, 48 or 64 columns;
            - for FMP-55X = 32 columns;
            - for DP-25X, DP-150X, WP-500X, WP-50X = 42 columns;
            */
            switch (modelName)
            {
                case "FP-700X":
                    return 48;
                case "FMP-350X":
                    return 48;
                case "FMP-55X":
                    return 32;
                default:
                    return 42;
            }
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

                if (!modelName.EndsWith("X", System.StringComparison.Ordinal))
                {
                    throw new InvalidDeviceInfoException($"incompatible with '{DriverName}'");
                }
            }
            var printColumns = getPrintColumnsOfModel(modelName);
            var info = new DeviceInfo
            {
                SerialNumber = serialNumber,
                FiscalMemorySerialNumber = commaFields[5],
                Model = modelName,
                FirmwareVersion = commaFields[1],
                Manufacturer = "Datecs",
                CommentTextMaxLength = printColumns - 2, // Set by Datecs X protocol
                ItemTextMaxLength = 72, // Set by Datecs X protocol
                OperatorPasswordMaxLength = 8 // Set by Datecs X protocol
            };


            return info;
        }
    }
}
