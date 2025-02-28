#nullable enable
namespace ErpNet.FP.Core.Drivers.BgDaisy
{
    using System;
    using System.Collections.Generic;
    using ErpNet.FP.Core.Configuration;
    using Serilog;

    public class BgDaisyIslFiscalPrinterDriver : FiscalPrinterDriver
    {

        protected readonly string SerialNumberPrefix = "DY";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.isl";


        public override IFiscalPrinter Connect(
            IChannel channel, 
            ServiceOptions serviceOptions, 
            bool autoDetect = true, 
            IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgDaisyIslFiscalPrinter(channel, serviceOptions, options);
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
                // Probing
                ParseDeviceInfo(rawDeviceInfo, autoDetect);
                // If there is no InvalidDeviceInfoException get the device info and constants
                var (rawDeviceConstants, _) = fiscalPrinter.GetRawDeviceConstants();
                fiscalPrinter.Info = ParseDeviceInfo(rawDeviceInfo, autoDetect, rawDeviceConstants);
                var (TaxIdentificationNumber, _) = fiscalPrinter.GetTaxIdentificationNumber();
                fiscalPrinter.Info.TaxIdentificationNumber = TaxIdentificationNumber;
                fiscalPrinter.Info.SupportedPaymentTypes = fiscalPrinter.GetSupportedPaymentTypes();
                fiscalPrinter.Info.SupportsSubTotalAmountModifiers = true;
                serviceOptions.ReconfigurePrinterConstants(fiscalPrinter.Info);
                return fiscalPrinter;
            }
        }

        protected DeviceInfo ParseDeviceInfo(string rawDeviceInfo, bool autoDetect, string? rawDeviceConstants = null)
        {
            var commaFields = rawDeviceInfo.Split(',');
            if (commaFields.Length != 6)
            {
                throw new InvalidDeviceInfoException($"rawDeviceInfo must contain 6 comma-separated items for '{DriverName}'");
            }
            var serialNumber = commaFields[4];
            if (autoDetect)
            {
                if (serialNumber.Length != 8 || !serialNumber.StartsWith(SerialNumberPrefix, System.StringComparison.Ordinal))
                {
                    throw new InvalidDeviceInfoException($"serial number must begin with {SerialNumberPrefix} and be with length 8 characters for '{DriverName}'");
                }
            }
            var spaceFields = commaFields[0].Split(' ');
            if (spaceFields.Length != 4)
            {
                throw new InvalidDeviceInfoException($"first member of comma separated list must contain 4 whitespace-separated values for '{DriverName}'");
            }
            // If probing only
            if (rawDeviceConstants == null)
            {
                // Return empty DeviceInfo
                return new DeviceInfo();
            }
            var commaConstants = rawDeviceConstants.Split(',');
            if (commaConstants.Length != 26)
            {
                throw new InvalidDeviceInfoException($"rawDeviceConstants must contain 25 comma-separated items for '{DriverName}'");
            }
            return new DeviceInfo
            {
                SerialNumber = serialNumber,
                FiscalMemorySerialNumber = commaFields[5],
                Model = spaceFields[0],
                FirmwareVersion = spaceFields[1],
                Manufacturer = "Daisy",
                CommentTextMaxLength = int.Parse(commaConstants[9]), // P10 max symbols per comment.
                ItemTextMaxLength = int.Parse(commaConstants[10]), // P11 max symbols for operator names, item names, department names.
                OperatorPasswordMaxLength = 6 // Set by Daisy protocol
            };
        }
    }
}