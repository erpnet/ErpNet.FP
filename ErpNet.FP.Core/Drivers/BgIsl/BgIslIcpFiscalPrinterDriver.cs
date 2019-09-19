using System;
using System.Collections.Generic;

#nullable enable
namespace ErpNet.FP.Core.Drivers.BgIcp
{
    public class BgIslIcpFiscalPrinterDriver : FiscalPrinterDriver
    {
        protected readonly string SerialNumberPrefix = "IS";
        public override string DriverName => $"bg.{SerialNumberPrefix.ToLower()}.icp";

        public override IFiscalPrinter Connect(IChannel channel, bool autoDetect = true, IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgIslIcpFiscalPrinter(channel, options);
            var rawDeviceInfoCacheKey = $"icp.{channel.Descriptor}";
            var rawDeviceInfo = Cache.Get(rawDeviceInfoCacheKey);
            if (rawDeviceInfo == null)
            {
                (rawDeviceInfo, _) = fiscalPrinter.GetRawDeviceInfo();
                Cache.Store(rawDeviceInfoCacheKey, rawDeviceInfo, TimeSpan.FromSeconds(30));
            }
            fiscalPrinter.Info = ParseDeviceInfo(rawDeviceInfo, autoDetect);
            fiscalPrinter.Info.SupportedPaymentTypes = fiscalPrinter.GetSupportedPaymentTypes();
            return fiscalPrinter;
        }

        protected int getPrintColumnsOfModel(string modelName)
        {
            /*
            За 57мм ISL3811.01,02,01М,02М – 32 символа
            За 80мм ISL3818 и 57мм ISL5011 – 47 символа
            За 80мм ISL5021 – 64 символа
            За 80мм ISL756 – 48 символа
            */
            if (modelName.StartsWith("ISL5011"))
            {
                return 47;
            }
            else if (modelName.StartsWith("ISL3818"))
            {
                return 47;
            }
            else if (modelName.StartsWith("ISL5021"))
            {
                return 64;
            }
            else if (modelName.StartsWith("ISL756"))
            {
                return 48;
            }
            else if (modelName.StartsWith("ISL3811"))
            {
                return 32;
            }
            return 47;
        }

        protected DeviceInfo ParseDeviceInfo(string rawDeviceInfo, bool autoDetect)
        {
            var tabFields = rawDeviceInfo.Split('\t', 2);
            if (tabFields.Length != 2)
            {
                throw new InvalidDeviceInfoException();
            }

            var fields = tabFields[0].Split(new int[] { 8, 8, 14, 4, 10, 1, 1 });
            if (fields.Length != 7)
            {
                throw new InvalidDeviceInfoException();
            }

            var spaceFields = tabFields[1].Split(' ', 2);
            if (spaceFields.Length != 2)
            {
                throw new InvalidDeviceInfoException();
            }

            var serialNumber = fields[0];
            var modelName = spaceFields[0];
            if (autoDetect)
            {
                if (serialNumber.Length != 8 || !serialNumber.StartsWith(SerialNumberPrefix, System.StringComparison.Ordinal))
                {
                    throw new InvalidDeviceInfoException($"serial number must begin with {SerialNumberPrefix} and be with length 8 characters for '{DriverName}'");
                }
            }
            var printColumns = getPrintColumnsOfModel(modelName);
            var info = new DeviceInfo
            {
                SerialNumber = serialNumber,
                FiscalMemorySerialNumber = fields[1],
                Model = modelName,
                FirmwareVersion = spaceFields[1],
                Manufacturer = "ISL",
                CommentTextMaxLength = printColumns - 2,
                ItemTextMaxLength = 40, // Set by Icp protocol
                OperatorPasswordMaxLength = 0,
                TaxIdentificationNumber = fields[2].Trim()
            };

            return info;
        }
    }
}
