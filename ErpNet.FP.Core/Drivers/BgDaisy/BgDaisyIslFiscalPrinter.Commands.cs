using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgDaisy
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Daisy Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgDaisyIslFiscalPrinter : BgIslFiscalPrinter
    {
        protected const byte
            DaisyCommandGetDeviceConstants = 0x80,
            DaisyCommandAbortFiscalReceipt = 0x82;

        public override (string, DeviceStatus) AbortReceipt()
        {
            return Request(DaisyCommandAbortFiscalReceipt);
        }

        public (string, DeviceStatus) GetRawDeviceConstants()

        {
            return Request(DaisyCommandGetDeviceConstants);
        }

        // 6 Bytes x 8 bits
        protected enum DeviceStatusBitsStringType { Error, Warning, Status, Reserved };

        protected static readonly (string, DeviceStatusBitsStringType)[] StatusBitsStrings = new[] {
            ("Syntax error", DeviceStatusBitsStringType.Error),
            ("Invalid command", DeviceStatusBitsStringType.Error),
            ("Date and time are not set", DeviceStatusBitsStringType.Error),
            ("No external display", DeviceStatusBitsStringType.Status),
            ("Error in printing device", DeviceStatusBitsStringType.Error),
            ("General error", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("Number field overflow", DeviceStatusBitsStringType.Error),
            ("Command not allowed in this mode", DeviceStatusBitsStringType.Error),
            ("Zeroed RAM", DeviceStatusBitsStringType.Warning),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            ("Error in cutter", DeviceStatusBitsStringType.Error),
            ("Wrong password", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("No paper", DeviceStatusBitsStringType.Error),
            ("Near end of paper", DeviceStatusBitsStringType.Warning),
            ("No control paper", DeviceStatusBitsStringType.Error),
            ("Opened Fiscal Receipt", DeviceStatusBitsStringType.Status),
            ("Control paper almost full", DeviceStatusBitsStringType.Warning),
            ("Opened Non-fiscal Receipt", DeviceStatusBitsStringType.Status),
            ("Printing allowed", DeviceStatusBitsStringType.Status),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            // Byte 3 is special in Daisy, it contains error code, from bit 0 to bit 6
            // bit 7 is reserved
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("Error while writing to FM", DeviceStatusBitsStringType.Error),
            ("No task from NRA", DeviceStatusBitsStringType.Error),
            ("Wrong record in FM", DeviceStatusBitsStringType.Error),
            ("FM almost full", DeviceStatusBitsStringType.Warning),
            ("FM full", DeviceStatusBitsStringType.Error),
            ("FM general error", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("FM overflow", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            ("VAT groups are set", DeviceStatusBitsStringType.Status),
            ("Device S/N and FM S/N are set", DeviceStatusBitsStringType.Status),
            ("FM ready", DeviceStatusBitsStringType.Status),
            (string.Empty, DeviceStatusBitsStringType.Reserved)
        };

        protected override DeviceStatus ParseStatus(byte[]? status)
        {
            var deviceStatus = new DeviceStatus();
            if (status == null)
            {
                return deviceStatus;
            }            
            for (var i = 0; i < status.Length; i++)
            {
                // Byte 3 is special in Daisy, it contains error code, from bit 0 to bit 6
                // bit 7 is reserved, so we will clear it from errorCode.
                if (i==3) 
                {
                    byte errorCode = (byte)(status[i] & 0b01111111);
                    if (errorCode > 0)
                    {
                        deviceStatus.Errors.Add($"Error code: {errorCode}, see Daisy Manual");
                    }
                    continue;
                }
                byte mask = 0b10000000;
                byte b = status[i];
                for (var j = 0; j < 8; j++)
                {
                    if ((mask & b) != 0)
                    {
                        var (statusBitString, statusBitStringType) = StatusBitsStrings[i * 8 + (7 - j)];
                        switch (statusBitStringType)
                        {
                            case DeviceStatusBitsStringType.Error:
                                deviceStatus.Errors.Add(statusBitString);
                                break;
                            case DeviceStatusBitsStringType.Warning:
                                deviceStatus.Warnings.Add(statusBitString);
                                break;
                            case DeviceStatusBitsStringType.Status:
                                deviceStatus.Statuses.Add(statusBitString);
                                break;
                            case DeviceStatusBitsStringType.Reserved:
                                break;
                        }
                    }
                    mask >>= 1;
                }
            }
            return deviceStatus;
        }

    }
}
