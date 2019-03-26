using ErpNet.FP.Print.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Print.Drivers.BgDaisy
{
    public class BgDaisyIslFiscalPrinterDriver : FiscalPrinterDriver
    {

        public override string SerialNumberPrefix => "DY";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.isl";


        public override IFiscalPrinter Connect(IChannel channel, IDictionary<string, string> options = null)
        {
            var fiscalPrinter = new BgDaisyIslFiscalPrinter(channel, options);
            var (rawDeviceInfo, _) = fiscalPrinter.GetRawDeviceInfo();
            var (rawDeviceConstants, _) = fiscalPrinter.GetRawDeviceConstants();
            fiscalPrinter.Info = ParseDeviceInfoAndConstants(rawDeviceInfo, rawDeviceConstants);
            return fiscalPrinter;
        }

        protected DeviceInfo ParseDeviceInfoAndConstants(string rawDeviceInfo, string rawDeviceConstants)
        {
            var commaFields = rawDeviceInfo.Split(',');
            if (commaFields.Length != 6)
            {
                throw new InvalidDeviceInfoException($"rawDeviceInfo must contain 6 comma-separated items for '{DriverName}'");
            }
            var serialNumber = commaFields[4];
            if (serialNumber.Length != 8 || !serialNumber.StartsWith(SerialNumberPrefix))
            {
                throw new InvalidDeviceInfoException($"serial number must begin with {SerialNumberPrefix} and be with length 8 characters for '{DriverName}'");
            }
            var spaceFields = commaFields[0].Split(' ');
            if (spaceFields.Length != 4)
            {
                throw new InvalidDeviceInfoException($"first member of comma separated list must contain 4 whitespace-separated values for '{DriverName}'");
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
                Company = "Daisy",
                CommentTextMaxLength = int.Parse(commaConstants[9]), // P10 max symbols per comment.
                ItemTextMaxLength = int.Parse(commaConstants[10]), // P11 max symbols for operator names, item names, department names.
                OperatorPasswordMaxLength = 6 // Set by Daisy protocol
            };
        }

    }
}