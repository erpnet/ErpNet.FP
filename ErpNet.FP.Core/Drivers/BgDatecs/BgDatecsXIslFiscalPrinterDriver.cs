#nullable enable
namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    using System;
    using System.Collections.Generic;
    using ErpNet.FP.Core.Configuration;
    using Serilog;

    /// <summary>
    /// Protocol for devices DP-25X, DP-05C, WP-500X, WP-50X, FP-700X, FP-700XR, FMP-350X, FMP-55X
    /// </summary>
    public class BgDatecsXIslFiscalPrinterDriver : FiscalPrinterDriver
    {
        protected readonly string SerialNumberPrefix = "DT";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.x.isl";

        public override IFiscalPrinter Connect(
            IChannel channel, 
            ServiceOptions serviceOptions, 
            bool autoDetect = true, 
            IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgDatecsXIslFiscalPrinter(channel, serviceOptions, options);
            var rawDeviceInfoCacheKey = $"x.isl.{channel.Descriptor}.{DriverName}";
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
                serviceOptions.ReconfigurePrinterOptions(fiscalPrinter.Info);
                return fiscalPrinter;
            }
        }

        protected int GetPrintColumnsOfModel(string modelName)
        {
            /*
            PrintColumns - Number of printer columns:
            - for FP-700X = 42, 48 or 64 columns;
            - for FMP-350X = 42, 48 or 64 columns;
            - for FMP-55X = 32 columns;
            - for DP-25X, DP-150X, WP-500X, WP-50X = 42 columns;
            */
            return modelName switch
            {
                "FP-700X" => 48,
                "FP-700XR" => 48,
                "FP-700XE" => 48,
                "FMP-350X" => 48,
                "FMP-55X" => 32,
                _ => 42,
            };
        }

        protected DeviceInfo ParseDeviceInfo(string rawDeviceInfo, bool autoDetect)
        {
            var commaFields = rawDeviceInfo.Split(new char[] { ',', ' ', '\t'});  // for compatibility reasons
            if (commaFields.Length != 8)
            {
                throw new InvalidDeviceInfoException($"rawDeviceInfo must contain 8 fields for '{DriverName}'");
            }
            var serialNumber = commaFields[6];
            var modelName = commaFields[0];
            if (autoDetect)
            {
                if (serialNumber.Length != 8 || !serialNumber.StartsWith(SerialNumberPrefix, System.StringComparison.Ordinal))
                {
                    throw new InvalidDeviceInfoException($"serial number must begin with {SerialNumberPrefix} and be with length 8 characters for '{DriverName}'");
                }

                if (!modelName.EndsWith("X", System.StringComparison.Ordinal) &&
                    !modelName.EndsWith("XR", System.StringComparison.Ordinal) &&
                    !modelName.EndsWith("XE", System.StringComparison.Ordinal))
                {
                    throw new InvalidDeviceInfoException($"incompatible with '{DriverName}'");
                }
            }
            var printColumns = GetPrintColumnsOfModel(modelName);
            var info = new DeviceInfo
            {
                SerialNumber = serialNumber,
                FiscalMemorySerialNumber = commaFields[7],
                Model = modelName,
                FirmwareVersion = $"{commaFields[1]} {commaFields[2]} {commaFields[3]}" ,
                Manufacturer = "Datecs",
                CommentTextMaxLength = printColumns - 2, // Set by Datecs X protocol
                ItemTextMaxLength = 72, // Set by Datecs X protocol
                OperatorPasswordMaxLength = 8, // Set by Datecs X protocol
                SupportPaymentTerminal = true
            };


            return info;
        }
    }
}
