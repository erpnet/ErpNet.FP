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

        public override string GetPaymentTypeText(PaymentType paymentType)
        {
            switch (paymentType)
            {
                case PaymentType.Cash:
                    return "P";
                case PaymentType.Card:
                    return "C";
                case PaymentType.Check:
                    return "N";
                case PaymentType.Reserved1:
                    return "D";
                default:
                    throw new StandardizedStatusMessageException($"Payment type {paymentType} unsupported", "E406");
            }
        }

        // 6 Bytes x 8 bits

        protected static readonly (string?, string, StatusMessageType)[] StatusBitsStrings = new (string?, string, StatusMessageType)[] {
            ("E401", "Syntax error", StatusMessageType.Error),
            ("E402", "Invalid command", StatusMessageType.Error),
            ("E103", "Date and time are not set", StatusMessageType.Error),
            (null, "No external display", StatusMessageType.Info),
            ("E303", "Error in printing device", StatusMessageType.Error),
            ("E199", "General error", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),

            ("E403", "Number field overflow", StatusMessageType.Error),
            ("E404", "Command not allowed in this mode", StatusMessageType.Error),
            ("E104", "Zeroed RAM", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            ("E306", "Error in cutter", StatusMessageType.Error),
            ("E408", "Wrong password", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),

            ("E301", "No paper", StatusMessageType.Error),
            ("W301", "Near end of paper", StatusMessageType.Warning),
            ("E206", "No control paper", StatusMessageType.Error),
            (null, "Opened Fiscal Receipt", StatusMessageType.Info),
            ("W202", "Control paper almost full", StatusMessageType.Warning),
            (null, "Opened Non-fiscal Receipt", StatusMessageType.Info),
            (null, "Printing allowed", StatusMessageType.Info),
            (null, string.Empty, StatusMessageType.Reserved),

            // Byte 3 is special in Daisy, it contains error code, from bit 0 to bit 6
            // bit 7 is reserved
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),

            ("E202", "Error while writing to FM", StatusMessageType.Error),
            ("E599", "No task from NRA", StatusMessageType.Error),
            ("E203", "Wrong record in FM", StatusMessageType.Error),
            ("W201", "FM almost full", StatusMessageType.Warning),
            ("E201", "FM full", StatusMessageType.Error),
            ("E299", "FM general error", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),

            ("E201", "FM overflow", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, "VAT groups are set", StatusMessageType.Info),
            (null, "Device S/N and FM S/N are set", StatusMessageType.Info),
            (null, "FM ready", StatusMessageType.Info),
            (null, string.Empty, StatusMessageType.Reserved)
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
                if (i == 3)
                {
                    byte errorCode = (byte)(status[i] & 0b01111111);
                    if (errorCode > 0)
                    {
                        deviceStatus.AddError("E999", $"Error code: {errorCode}, see Daisy Manual");
                    }
                    continue;
                }
                byte mask = 0b10000000;
                byte b = status[i];
                for (var j = 0; j < 8; j++)
                {
                    if ((mask & b) != 0)
                    {
                        var (statusBitsCode, statusBitsText, statusBitStringType) = StatusBitsStrings[i * 8 + (7 - j)];
                        deviceStatus.AddMessage(new StatusMessage
                        {
                            Type = statusBitStringType,
                            Code = statusBitsCode,
                            Text = statusBitsText
                        });
                    }
                    mask >>= 1;
                }
            }
            return deviceStatus;
        }

    }
}
