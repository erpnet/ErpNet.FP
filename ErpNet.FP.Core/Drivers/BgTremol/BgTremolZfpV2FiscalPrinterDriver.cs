using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgTremol
{
    public class BgTremolZfpV2FiscalPrinterDriver : FiscalPrinterDriver
    {

        protected readonly string SerialNumberPrefix = "ZK";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.v2.zfp";

        public override IFiscalPrinter Connect(IChannel channel, IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgTremolZfpV2FiscalPrinter(channel, options);
            var (rawDeviceInfo, _) = fiscalPrinter.GetRawDeviceInfo();
            fiscalPrinter.Info = ParseDeviceInfo(rawDeviceInfo);
            var (TaxIdentificationNumber, _) = fiscalPrinter.GetTaxIdentificationNumber();
            fiscalPrinter.Info.TaxIdentificationNumber = TaxIdentificationNumber;
            return fiscalPrinter;
        }

        protected DeviceInfo ParseDeviceInfo(string rawDeviceInfo)
        {
            // Example: 1;784;04-02-2019 08:00;TREMOL M20; Ver. 1.01 TRA20 C.S. 2541;ZK126720;50163145
            var fields = rawDeviceInfo.Split(';');
            if (fields.Length != 7)
            {
                throw new InvalidDeviceInfoException($"rawDeviceInfo must contain 7 comma-separated items for '{DriverName}'");
            }
            var serialNumber = fields[5];
            if (serialNumber.Length != 8 || !serialNumber.StartsWith(SerialNumberPrefix))
            {
                throw new InvalidDeviceInfoException($"serial number must begin with {SerialNumberPrefix} and be with length 8 characters for '{DriverName}'");
            }
            var model = fields[3].Replace("TREMOL ", "");
            if (!model.EndsWith("V2"))
            {
                throw new InvalidDeviceInfoException($"model should have a suffix 'V2'");
            }
            var info = new DeviceInfo
            {
                SerialNumber = serialNumber,
                FiscalMemorySerialNumber = fields[6],
                Model = model,
                FirmwareVersion = fields[4].Trim(),
                Manifacturer = "Tremol",
                CommentTextMaxLength = 30,
                ItemTextMaxLength = 32,
                OperatorPasswordMaxLength = 6
            };
            return info;
        }
    }
}
