using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Datecs Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgDatecsCIslFiscalPrinter : BgIslFiscalPrinter
    {
        public override (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber)
        {
            var header = string.Join(",",
                new string[] {
                    Options.ValueOrDefault("Operator.ID", "1"),
                    Options.ValueOrDefault("Operator.Password", "1").WithMaxLength(Info.OperatorPasswordMaxLength),
                    uniqueSaleNumber,
                    "1"
                });
            return Request(CommandOpenFiscalReceipt, header);
        }

        public override string GetPaymentTypeText(PaymentType paymentType)
        {
            switch (paymentType)
            {
                case PaymentType.Cash:
                    return "P";
                case PaymentType.Coupon:
                    return "J";
                case PaymentType.Voucher:
                    return "I";
                case PaymentType.Card:
                    return "C";
                case PaymentType.Reserved1:
                    return "D"; // National Health Insurance Fund
                default:
                    return "P";
            }
        }

        // 6 Bytes x 8 bits
        protected static readonly (string, DeviceStatusBitsStringType)[] StatusBitsStrings = new[] {
            ("Syntax error in the received data", DeviceStatusBitsStringType.Error),
            ("Invalid command code received", DeviceStatusBitsStringType.Error),
            ("The clock is not set", DeviceStatusBitsStringType.Error),
            ("No customer display is connected", DeviceStatusBitsStringType.Status),
            ("Printing unit fault", DeviceStatusBitsStringType.Error),
            ("General error", DeviceStatusBitsStringType.Error),
            ("The printer cover is open", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("The command resulted in an overflow of some amount fields", DeviceStatusBitsStringType.Error),
            ("The command is not allowed in the current fiscal mode", DeviceStatusBitsStringType.Error),
            ("The RAM has been reset", DeviceStatusBitsStringType.Error),
            ("Low battery (the real-time clock is in RESET status)", DeviceStatusBitsStringType.Error),
            ("A refund (storno) receipt is open", DeviceStatusBitsStringType.Status),
            ("A service receipt with 90-degree rotated text printing is open", DeviceStatusBitsStringType.Status),
            ("The built-in tax terminal is not responding", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("No paper", DeviceStatusBitsStringType.Error),
            ("Low paper", DeviceStatusBitsStringType.Warning),
            ("End of the EJ", DeviceStatusBitsStringType.Error),
            ("A fiscal receipt is open", DeviceStatusBitsStringType.Status),
            ("The end of the EJ is near", DeviceStatusBitsStringType.Warning),
            ("A service receipt is open", DeviceStatusBitsStringType.Status),
            ("The end of the EJ is very near", DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            // Byte 3, bits from 0 to 6 are SW 1 to 7
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("Fiscal memory store error", DeviceStatusBitsStringType.Error),
            ("BULSTAT UIC is set", DeviceStatusBitsStringType.Status),
            ("Unique Printer ID and Fiscal Memory ID are set", DeviceStatusBitsStringType.Status),
            ("There is space for less than 50 records remaining in the FP", DeviceStatusBitsStringType.Warning),
            ("The fiscal memory is full", DeviceStatusBitsStringType.Error),
            ("FM general error", DeviceStatusBitsStringType.Error),
            ("The printing head is overheated", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("The fiscal memory is set in READONLY mode (locked)", DeviceStatusBitsStringType.Error),
            ("The fiscal memory is formatted", DeviceStatusBitsStringType.Status),
            ("The last fiscal memory store operation is not successful", DeviceStatusBitsStringType.Error),
            ("The printer is in fiscal mode", DeviceStatusBitsStringType.Status),
            ("The tax rates are set at least once", DeviceStatusBitsStringType.Status),
            ("Fiscal memory read error", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
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
                byte mask = 0b10000000;
                byte b = status[i];
                // Byte 3 shows the switches SW1 .. SW7 state
                if (i == 3)
                {
                    var switchData = new List<string>();
                    // Skip bit 7
                    for (var j = 0; j < 7; j++)
                    {
                        var switchState = ((mask & b) != 0) ? "ON" : "OFF";
                        switchData.Add($"SW{7 - j}={switchState}");
                        mask >>= 1;
                    }
                    deviceStatus.Statuses.Add(string.Join(", ", switchData));
                }
                else
                {
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
            }
            return deviceStatus;
        }

    }
}
