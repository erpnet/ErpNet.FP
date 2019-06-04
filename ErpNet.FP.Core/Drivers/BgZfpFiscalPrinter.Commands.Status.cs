namespace ErpNet.FP.Core.Drivers
{
    /// <summary>
    /// Fiscal printer using the Zfp implementation of Tremol Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgZfpFiscalPrinter" />
    public partial class BgZfpFiscalPrinter : BgFiscalPrinter
    {
        // Begins at 0x30, ends at 0x3f
        // All strings are errors
        protected static readonly (string?, string)[] FiscalDeviceErrors = new (string?, string)[]{
            (null, string.Empty), // No error
            ("E301", "Out of paper, printer failure"),
            ("E403", "Registers overflow"),
            ("E103", "Clock failure or incorrect date & time"),
            ("E404", "Opened fiscal receipt"),
            ("E406", "Payment residue ammount"),
            ("E404", "Opened non-fiscal receipt"),
            ("E405", "Registered payment but receipt is not closed"),
            ("E299", "Fiscal memory failure"),
            ("E408", "Incorrect password"),
            ("E105", "Missing external display"),
            ("E502", "24hours block – missing Z report"),
            ("E304", "Overheated printer thermal head"),
            ("E305", "Interrupt power supply in fiscal receipt(one time until status is read)"),
            ("E206", "Overflow EJ"),
            ("E405", "Insufficient conditions")
        };

        // Begins at 0x30, ends at 0x38
        // All strings are errors
        protected static readonly (string?, string)[] CommandErrors = new (string?, string)[] {
            (null, string.Empty), // No error
            ("E402", "Invalid command"),
            ("E404", "Illegal command"),
            ("E405", "Z daily report is not zero"),
            ("E401", "Syntax error"),
            ("E403", "Input registers overflow"),
            ("E405", "Zero input registers"),
            ("E405", "Unavailable transaction for correction"),
            ("E405", "Insufficient amount on hand")
        };

        // 7 Bytes x 8 bits

        protected static readonly (string?, string, StatusMessageType)[] StatusBitsStrings = new (string?, string, StatusMessageType)[] {
            ("E204", "FM Read only", StatusMessageType.Error),
            ("E305", "Power down in opened fiscal receipt", StatusMessageType.Error),
            ("E304", "Printer not ready - overheat", StatusMessageType.Error),
            ("E103", "DateTime not set", StatusMessageType.Error),
            ("E103", "DateTime wrong", StatusMessageType.Error),
            ("E104", "RAM reset", StatusMessageType.Error),
            ("E103", "Hardware clock error", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),

            ("E301", "Printer not ready - no paper", StatusMessageType.Error),
            ("E403", "Report registers overflow", StatusMessageType.Error),
            ("W502", "Customer report is not zeroed", StatusMessageType.Warning),
            ("W502", "Daily report is not zeroed", StatusMessageType.Warning),
            ("W502", "Article report is not zeroed", StatusMessageType.Warning),
            ("W502", "Operator report is not zeroed", StatusMessageType.Warning),
            (null, "Duplicate printed", StatusMessageType.Info),
            (null, string.Empty,StatusMessageType.Reserved),

            (null, "Opened Non-fiscal Receipt", StatusMessageType.Info),
            (null, "Opened Fiscal Receipt", StatusMessageType.Info),
            (null, "Opened Fiscal Detailed Receipt", StatusMessageType.Info),
            (null, "Opened Fiscal Receipt with VAT", StatusMessageType.Info),
            (null, "Opened Invoice Fiscal Receipt", StatusMessageType.Info),
            ("W202", "SD card near full", StatusMessageType.Warning),
            ("E206", "SD card full", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),

            ("E205", "No FM module", StatusMessageType.Error),
            ("E299", "FM error", StatusMessageType.Error),
            ("E201", "FM full", StatusMessageType.Error),
            ("W201", "FM near full", StatusMessageType.Warning),
            (null, "Decimal point(1=fract, 0=whole)", StatusMessageType.Info),
            (null, "FM fiscalized", StatusMessageType.Info),
            (null, "FM produced", StatusMessageType.Info),
            (null, string.Empty, StatusMessageType.Reserved),

            (null, "Printer: automatic cutting", StatusMessageType.Info),
            (null, "External display: transparent display", StatusMessageType.Info),
            (null, "Speed is 9600", StatusMessageType.Info),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, "Drawer: automatic opening", StatusMessageType.Info),
            (null, "Customer logo included in the receipt", StatusMessageType.Info),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),

            ("E504", "Wrong SIM card", StatusMessageType.Error),
            ("E503", "Blocking 3 days without mobile operator", StatusMessageType.Error),
            ("E501", "No task from NRA", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            ("E207", "Wrong SD card", StatusMessageType.Error),
            ("E599", "Deregistered", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),

            ("E504", "No SIM card", StatusMessageType.Error),
            ("E507", "No GPRS Modem", StatusMessageType.Error),
            ("E506", "No mobile operator", StatusMessageType.Error),
            ("E505", "No GPRS service", StatusMessageType.Error),
            ("W301", "Near end of paper", StatusMessageType.Warning),
            ("W501", "Unsent data for 24 hours", StatusMessageType.Warning),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved)
        };

        protected virtual DeviceStatus ParseCommandStatus(byte[] status)
        {
            var deviceStatus = new DeviceStatus();
            // Byte 0
            var (errorCode, errorText) = FiscalDeviceErrors[status[0] - 0x30];
            if (errorText.Length > 0 && errorCode != null)
            {
                deviceStatus.AddError(errorCode, errorText);
            }
            // Byte 1
            (errorCode, errorText) = CommandErrors[status[1] - 0x30];
            if (errorText.Length > 0 && errorCode != null)
            {
                deviceStatus.AddError(errorCode, errorText);
            }
            return deviceStatus;
        }

        protected virtual DeviceStatus ParseDeviceStatus(byte[] status)
        {
            var deviceStatus = new DeviceStatus();
            for (var i = 0; i < status.Length; i++)
            {
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

        protected override DeviceStatus ParseStatus(byte[]? status)
        {
            if (status != null)
            {
                switch (status.Length)
                {
                    case 2: // ACK Status is 2 bytes
                        return ParseCommandStatus(status);
                    case 7: // Device Status is 7 bytes
                        return ParseDeviceStatus(status);
                }
            }

            return new DeviceStatus();

            /*
            There are two different variants of statuses in the protocol
            1. ACK Status (received after every command) 
            2. Device Status (availble with command GetStatus)

            ****************************************************************
            1. ACK Statuses are Fiscal Device Errors and Command Errors

            Byte 0 - Fiscal Device Errors, Values (hex):
            30 OK
            31 Out of paper, printer failure
            32 Registers overflow
            33 Clock failure or incorrect date & time 
            34 Opened fiscal receipt 
            35 Payment residue account            
            36 Opened non-fiscal receipt 
            37 Registered payment but receipt is not closed            
            38 Fiscal memory failure            
            39 Incorrect password
            3a Missing external display
            3b 24hours block – missing Z report
            3c Overheated printer thermal head.
            3d Interrupt power supply in fiscal receipt(one time until status is read)
            3e Overflow EJ
            3f Insufficient conditions

            Byte 1 - Command Errors, Values (hex):
            30 OK
            31 Invalid command
            32 Illegal command
            33 Z daily report is not zero
            34 Syntax error
            35 Input registers overflow
            36 Zero input registers
            37 Unavailable transaction for correction
            38 Insufficient amount on hand

            ****************************************************************
            2. Device Statuses

            Byte 0, Bits:
            0 FM Read only
            1 Power down in opened fiscal receipt
            2 Printer not ready - overheat
            3 DateTime not set
            4 DateTime wrong
            5 RAM reset
            6 Hardware clock error
            7 Always 1 - Reserved

            Byte 1, Bits:
            0 Printer not ready - no paper
            1 Reports registers Overflow
            2 Customer report is not zeroed
            3 Daily report is not zeroed
            4 Article report is not zeroed
            5 Operator report is not zeroed
            6 Duplicate printed
            7 Always 1 - Reserved

            Byte 2, Bits:
            0 Opened Non-fiscal Receipt
            1 Opened Fiscal Receipt
            2 Opened Fiscal Detailed Receipt
            3 Opened Fiscal Receipt with VAT
            4 Opened Invoice Fiscal Receipt
            5 SD card near full
            6 SD card full
            7 Always 1 - Reserved

            Byte 3, Bits:
            0 No FM module
            1 FM error
            2 FM full
            3 FM near full
            4 Decimal point (1=fract, 0=whole)
            5 FM fiscalized
            6 FM produced
            7 Always 1 - Reserved

            Byte 4, Bits:
            0 Printer: automatic cutting
            1 External display: transparent display
            2 Speed is 9600
            3 reserve
            4 Drawer: automatic opening
            5 Customer logo included in the receipt
            6 reserve
            7 Always 1 - Reserved

            Byte 5, Bits:
            0 Wrong SIM card
            1 Blocking 3 days without mobile operator
            2 No task from NRA
            3 reserved
            4 reserved
            5 Wrong SD card
            6 Deregistered
            7 Always 1 - Reserved

            Byte 6, Bits:
            0 No SIM card
            1 No GPRS Modem
            2 No mobile operator
            3 No GPRS service
            4 Near end of paper
            5 Unsent data for 24 hours
            6 reserved
            7 Always 1 - Reserved

            */
        }

    }
}
