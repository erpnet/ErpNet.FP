#nullable enable
namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    using System;
    using System.Collections.Generic;
    using ErpNet.FP.Core.Configuration;

    public class BgDatecsPIslFiscalPrinterDriver : FiscalPrinterDriver
    {
        protected readonly string SerialNumberPrefix = "DT";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.p.isl";

        public override IFiscalPrinter Connect(
            IChannel channel, 
            ServiceOptions serviceOptions, 
            bool autoDetect = true, 
            IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgDatecsPIslFiscalPrinter(channel, serviceOptions, options);
            var rawDeviceInfoCacheKey = $"isl.{channel.Descriptor}";
            var rawDeviceInfo = Cache.Get(rawDeviceInfoCacheKey);
            if (rawDeviceInfo == null)
            {
                (rawDeviceInfo, _) = fiscalPrinter.GetRawDeviceInfo();
                Cache.Store(rawDeviceInfoCacheKey, rawDeviceInfo, TimeSpan.FromSeconds(30));
            }
            fiscalPrinter.Info = ParseDeviceInfo(rawDeviceInfo, autoDetect);
            var (TaxIdentificationNumber, _) = fiscalPrinter.GetTaxIdentificationNumber();
            fiscalPrinter.Info.TaxIdentificationNumber = TaxIdentificationNumber;
            fiscalPrinter.Info.SupportedPaymentTypes = fiscalPrinter.GetSupportedPaymentTypes();
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
                CommentTextMaxLength = 40, // Set 42 by Datecs protocol, but in reality is 40 symbols
                ItemTextMaxLength = 34, // Set by 36 Datecs protocol, but in reality is 34 symbols
                OperatorPasswordMaxLength = 8 // Set by Datecs protocol
            };
            return info;
        }
    }
}
