using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgIncotex
{
    public class BgIncotexIslFiscalPrinterDriver : FiscalPrinterDriver
    {

        protected readonly string SerialNumberPrefix = "IN";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.isl";


        public override IFiscalPrinter Connect(IChannel channel, IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgIncotexIslFiscalPrinter(channel, options);
            var (rawDeviceInfo, _) = fiscalPrinter.GetRawDeviceInfo();
            try
            {
                // Probing
                ParseDeviceInfo(rawDeviceInfo);
                // If there is no InvalidDeviceInfoException get the device info and constants
                var (rawDeviceConstants, _) = fiscalPrinter.GetRawDeviceConstants();
                fiscalPrinter.Info = ParseDeviceInfo(rawDeviceInfo, rawDeviceConstants);
                var (TaxIdentificationNumber, _) = fiscalPrinter.GetTaxIdentificationNumber();
                fiscalPrinter.Info.TaxIdentificationNumber = TaxIdentificationNumber;
            }
            catch (InvalidDeviceInfoException e)
            {
                throw e;
            }
            return fiscalPrinter;
        }

        protected DeviceInfo ParseDeviceInfo(string rawDeviceInfo, string? rawDeviceConstants = null)
        {
            // 2.11 Jan 22 2019 14:00,DB44EEAD,0000,06,IN015013,54015013,284013911622147
            var commaFields = rawDeviceInfo.Split(',');
            if (commaFields.Length < 7)
            {
                throw new InvalidDeviceInfoException($"rawDeviceInfo must contain 7 comma-separated items for '{DriverName}'");
            }
            var serialNumber = commaFields[4];
            if (serialNumber.Length != 8 || !serialNumber.StartsWith(SerialNumberPrefix))
            {
                throw new InvalidDeviceInfoException($"serial number must begin with {SerialNumberPrefix} and be with length 8 characters for '{DriverName}'");
            }
            // If probing only
            if (rawDeviceConstants == null)
            {
                // Return empty DeviceInfo
                return new DeviceInfo();
            }
            var commaConstants = rawDeviceConstants.Split(',');
            if (commaConstants.Length < 10)
            {
                throw new InvalidDeviceInfoException($"rawDeviceConstants must contain 25 comma-separated items for '{DriverName}'");
            }
            return new DeviceInfo
            {
                SerialNumber = serialNumber,
                FiscalMemorySerialNumber = commaFields[5],
                Model = "EFD",
                FirmwareVersion = commaFields[0],
                Manifacturer = "Incotex",
                CommentTextMaxLength = int.Parse(commaConstants[9]), // P10 max symbols per comment.
                ItemTextMaxLength = int.Parse(commaConstants[10]), // P11 max symbols for operator names, item names, department names.
                OperatorPasswordMaxLength = 6 // Set by Incotex protocol
            };
        }
    }
}