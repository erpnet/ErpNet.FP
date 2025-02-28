﻿#nullable enable
namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    using System;
    using System.Collections.Generic;
    using ErpNet.FP.Core.Configuration;
    using Serilog;

    public class BgDatecsCIslFiscalPrinterDriver : FiscalPrinterDriver
    {
        protected readonly string SerialNumberPrefix = "DT";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.c.isl";

        public override IFiscalPrinter Connect(
            IChannel channel, 
            ServiceOptions serviceOptions, 
            bool autoDetect = true, 
            IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgDatecsCIslFiscalPrinter(channel, serviceOptions, options);
            var rawDeviceInfoCacheKey = $"isl.{channel.Descriptor}.{DriverName}";
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

                if (modelName.EndsWith("X", System.StringComparison.Ordinal) ||
                    modelName.EndsWith("XR", System.StringComparison.Ordinal) ||
                    modelName.EndsWith("XE", System.StringComparison.Ordinal))
                {
                    throw new InvalidDeviceInfoException($"model not in (X,XR,XE) incompatible with '{DriverName}'");
                }

                if (
                    !modelName.StartsWith("DP", System.StringComparison.Ordinal) &&
                    !modelName.StartsWith("WP", System.StringComparison.Ordinal))
                {
                    throw new InvalidDeviceInfoException($"model not in (DP,WP) incompatible with '{DriverName}'");
                }
            }
            var info = new DeviceInfo
            {
                SerialNumber = serialNumber,
                FiscalMemorySerialNumber = commaFields[5],
                Model = modelName,
                FirmwareVersion = commaFields[1],
                Manufacturer = "Datecs",
                CommentTextMaxLength = 36, // Set 40 by Datecs protocol, 
                                           // but the reality is 36 symbols, 
                                           // otherwise the comment will be cut out 
                                           // by the Datecs firmware after 36th symbol
                ItemTextMaxLength = 22, // Set by Datecs protocol
                OperatorPasswordMaxLength = 8 // Set by Datecs protocol
            };
            return info;
        }
    }
}
