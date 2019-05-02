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
        protected static readonly string[] FiscalDeviceErrors = {
            string.Empty, // No error
            "Out of paper, printer failure",
            "Registers overflow",
            "Clock failure or incorrect date & time",
            "Opened fiscal receipt",
            "Payment residue account",
            "Opened non-fiscal receipt",
            "Registered payment but receipt is not closed",
            "Fiscal memory failure",
            "Incorrect password",
            "Missing external display",
            "24hours block – missing Z report",
            "Overheated printer thermal head",
            "Interrupt power supply in fiscal receipt(one time until status is read)",
            "Overflow EJ",
            "Insufficient conditions"
        };

        // Begins at 0x30, ends at 0x38
        // All strings are errors
        protected static readonly string[] CommandErrors = {
            string.Empty, // No error
            "Invalid command",
            "Illegal command",
            "Z daily report is not zero",
            "Syntax error",
            "Input registers overflow",
            "Zero input registers",
            "Unavailable transaction for correction",
            "Insufficient amount on hand"
        };

        // 7 Bytes x 8 bits

        protected static readonly (string, DeviceStatusBitsStringType)[] StatusBitsStrings = new[] {
            ("FM Read only", DeviceStatusBitsStringType.Error),
            ("Power down in opened fiscal receipt", DeviceStatusBitsStringType.Error),
            ("Printer not ready - overheat", DeviceStatusBitsStringType.Error),
            ("DateTime not set", DeviceStatusBitsStringType.Error),
            ("DateTime wrong", DeviceStatusBitsStringType.Error),
            ("RAM reset", DeviceStatusBitsStringType.Error),
            ("Hardware clock error", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("Printer not ready - no paper", DeviceStatusBitsStringType.Error),
            ("Reports registers Overflow", DeviceStatusBitsStringType.Error),
            ("Customer report is not zeroed", DeviceStatusBitsStringType.Warning),
            ("Daily report is not zeroed", DeviceStatusBitsStringType.Warning),
            ("Article report is not zeroed", DeviceStatusBitsStringType.Warning),
            ("Operator report is not zeroed", DeviceStatusBitsStringType.Warning),
            ("Duplicate printed", DeviceStatusBitsStringType.Status),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("Opened Non-fiscal Receipt", DeviceStatusBitsStringType.Status),
            ("Opened Fiscal Receipt", DeviceStatusBitsStringType.Status),
            ("Opened Fiscal Detailed Receipt", DeviceStatusBitsStringType.Status),
            ("Opened Fiscal Receipt with VAT", DeviceStatusBitsStringType.Status),
            ("Opened Invoice Fiscal Receipt", DeviceStatusBitsStringType.Status),
            ("SD card near full", DeviceStatusBitsStringType.Warning),
            ("SD card full", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("No FM module", DeviceStatusBitsStringType.Error),
            ("FM error", DeviceStatusBitsStringType.Error),
            ("FM full", DeviceStatusBitsStringType.Error),
            ("FM near full", DeviceStatusBitsStringType.Warning),
            ("Decimal point(1=fract, 0=whole)", DeviceStatusBitsStringType.Status),
            ("FM fiscalized", DeviceStatusBitsStringType.Status),
            ("FM produced", DeviceStatusBitsStringType.Status),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("Printer: automatic cutting", DeviceStatusBitsStringType.Status),
            ("External display: transparent display", DeviceStatusBitsStringType.Status),
            ("Speed is 9600", DeviceStatusBitsStringType.Status),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            ("Drawer: automatic opening", DeviceStatusBitsStringType.Status),
            ("Customer logo included in the receipt", DeviceStatusBitsStringType.Status),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("Wrong SIM card", DeviceStatusBitsStringType.Error),
            ("Blocking 3 days without mobile operator", DeviceStatusBitsStringType.Error),
            ("No task from NRA", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            ("Wrong SD card", DeviceStatusBitsStringType.Error),
            ("Deregistered", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("No SIM card", DeviceStatusBitsStringType.Error),
            ("No GPRS Modem", DeviceStatusBitsStringType.Error),
            ("No mobile operator", DeviceStatusBitsStringType.Error),
            ("No GPRS service", DeviceStatusBitsStringType.Error),
            ("Near end of paper", DeviceStatusBitsStringType.Warning),
            ("Unsent data for 24 hours", DeviceStatusBitsStringType.Warning),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved)
        };

        protected virtual DeviceStatus ParseCommandStatus(byte[] status)
        {
            var deviceStatus = new DeviceStatus();
            // Byte 0
            var fiscalDeviceError = FiscalDeviceErrors[status[0] - 0x30];
            if (fiscalDeviceError.Length > 0)
            {
                deviceStatus.Errors.Add(fiscalDeviceError);
            }
            // Byte 1
            var commandError = CommandErrors[status[1] - 0x30];
            if (commandError.Length > 0)
            {
                deviceStatus.Errors.Add(commandError);
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
