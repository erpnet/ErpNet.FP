using System;
using System.Collections.Generic;
using ErpNet.FP.Core;

namespace ErpNet.FP.Core.Drivers.BgTremol
{
    public class BgTremolZfpFiscalPrinterDriver : FiscalPrinterDriver
    {

        public override string SerialNumberPrefix => "ZK";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.zfp";

        public override IFiscalPrinter Connect(IChannel channel, IDictionary<string, string> options = null)
        {
            var fiscalPrinter = new BgTremolZfpFiscalPrinter(channel, options);
            var (rawDeviceInfo, _) = fiscalPrinter.GetRawDeviceInfo();
            fiscalPrinter.Info = ParseDeviceInfo(rawDeviceInfo);
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
            var info = new DeviceInfo
            {
                SerialNumber = serialNumber,
                FiscalMemorySerialNumber = fields[6],
                Model = fields[3].Replace("TREMOL ", ""), // Clear TREMOL from model name, to avoid redundancy
                FirmwareVersion = fields[4],
                Company = "Tremol",
                CommentTextMaxLength = 46, // Set by Eltrade protocol
                ItemTextMaxLength = 30, // Set by Eltrade protocol
                OperatorPasswordMaxLength = 6 // Set by Eltrade protocol
            };
            return info;
        }
    }
}
