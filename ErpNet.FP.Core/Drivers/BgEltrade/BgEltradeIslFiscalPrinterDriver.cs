#nullable enable
namespace ErpNet.FP.Core.Drivers.BgEltrade
{
    using System;
    using System.Collections.Generic;

    public class BgEltradeIslFiscalPrinterDriver : FiscalPrinterDriver
    {
        protected readonly string SerialNumberPrefix = "ED";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.isl";

        public override IFiscalPrinter Connect(IChannel channel, bool autoDetect = true, IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgEltradeIslFiscalPrinter(channel, options);
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
            if (commaFields.Length != 7)
            {
                throw new InvalidDeviceInfoException($"rawDeviceInfo must contain 7 comma-separated items for '{DriverName}'");
            }
            var serialNumber = commaFields[5];
            if (autoDetect)
            {
                if (serialNumber.Length != 8 || !serialNumber.StartsWith(SerialNumberPrefix, System.StringComparison.Ordinal))
                {
                    throw new InvalidDeviceInfoException($"serial number must begin with {SerialNumberPrefix} and be with length 8 characters for '{DriverName}'");
                }
            }
            var info = new DeviceInfo
            {
                SerialNumber = serialNumber,
                FiscalMemorySerialNumber = commaFields[6],
                Model = commaFields[0],
                FirmwareVersion = commaFields[2],
                Manufacturer = "Eltrade",
                CommentTextMaxLength = 46, // Set by Eltrade protocol
                ItemTextMaxLength = 30, // Set by Eltrade protocol
                OperatorPasswordMaxLength = 8 // Set by Eltrade protocol
            };
            return info;
        }

    }
}
