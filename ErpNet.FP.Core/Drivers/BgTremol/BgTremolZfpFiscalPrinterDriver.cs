#nullable enable
namespace ErpNet.FP.Core.Drivers.BgTremol
{
    using System;
    using System.Collections.Generic;
    using ErpNet.FP.Core.Configuration;
    using Serilog;

    public class BgTremolZfpFiscalPrinterDriver : FiscalPrinterDriver
    {

        protected readonly string SerialNumberPrefix = "ZK";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.zfp";

        public override IFiscalPrinter Connect(
            IChannel channel, 
            ServiceOptions serviceOptions, 
            bool autoDetect = true, 
            IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgTremolZfpFiscalPrinter(channel, serviceOptions, options);
            var rawDeviceInfoCacheKey = $"zfp.{channel.Descriptor}.{DriverName}";
            lock (channel)
            {
                var rawDeviceInfo = Cache.Get(rawDeviceInfoCacheKey);
                if (rawDeviceInfo == null)
                {
                    (rawDeviceInfo, _) = fiscalPrinter.GetRawDeviceInfo();
                    Log.Information($"RawDeviceInfo({channel.Descriptor}): {rawDeviceInfo}");
                    Cache.Store(rawDeviceInfoCacheKey, rawDeviceInfo, TimeSpan.FromSeconds(30));
                }
                fiscalPrinter.Info = ParseDeviceInfo(rawDeviceInfo, autoDetect);
                var (TaxIdentificationNumber, _) = fiscalPrinter.GetTaxIdentificationNumber();
                fiscalPrinter.Info.TaxIdentificationNumber = TaxIdentificationNumber;
                fiscalPrinter.Info.SupportedPaymentTypes = fiscalPrinter.GetSupportedPaymentTypes();
                fiscalPrinter.Info.SupportsSubTotalAmountModifiers = true;
                serviceOptions.ReconfigurePrinterConstants(fiscalPrinter.Info);
                return fiscalPrinter;
            }
        }

        protected DeviceInfo ParseDeviceInfo(string rawDeviceInfo, bool autoDetect)
        {
            // Example: 1;784;04-02-2019 08:00;TREMOL M20; Ver. 1.01 TRA20 C.S. 2541;ZK126720;50163145
            var fields = rawDeviceInfo.Split(';');
            if (fields.Length != 7)
            {
                throw new InvalidDeviceInfoException($"rawDeviceInfo must contain 7 comma-separated items for '{DriverName}'");
            }
            var serialNumber = fields[5];
            var model = fields[3].Replace("TREMOL ", "");
            if (autoDetect)
            {
                if (serialNumber.Length != 8 || !serialNumber.StartsWith(SerialNumberPrefix, System.StringComparison.Ordinal))
                {
                    throw new InvalidDeviceInfoException($"serial number must begin with {SerialNumberPrefix} and be with length 8 characters for '{DriverName}'");
                }

                if (model.EndsWith("V2", System.StringComparison.Ordinal))
                {
                    throw new InvalidDeviceInfoException($"model should NOT have a suffux 'V2'");
                }
            }
            var info = new DeviceInfo
            {
                SerialNumber = serialNumber,
                FiscalMemorySerialNumber = fields[6],
                Model = model,
                FirmwareVersion = fields[4].Trim(),
                Manufacturer = "Tremol",
                CommentTextMaxLength = 30,
                ItemTextMaxLength = 32,
                OperatorPasswordMaxLength = 6
            };
            return info;
        }
    }
}
