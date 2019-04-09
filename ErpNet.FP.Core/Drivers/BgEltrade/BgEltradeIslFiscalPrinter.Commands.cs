using ErpNet.FP.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgEltrade
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Eltrade Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgEltradeIslFiscalPrinter : BgIslFiscalPrinter
    {
        protected const byte
            EltradeCommandOpenFiscalReceipt = 0x90;

        public override (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber)
        {
            var header = string.Join(",",
                new string[] {
                    Options.ValueOrDefault("Operator.Name", "Operator"),
                    uniqueSaleNumber
                });
            return Request(CommandOpenFiscalReceipt, header);
        }

        // 6 Bytes x 8 bits
        protected static readonly (string, DeviceStatusBitsStringType)[] StatusBitsStrings = new[] {
            ("Incoming data has syntax error", DeviceStatusBitsStringType.Error),
            ("Code of incoming command is invalid", DeviceStatusBitsStringType.Error),
            ("The clock needs setting", DeviceStatusBitsStringType.Error),
            ("Not connected a customer display", DeviceStatusBitsStringType.Status),
            ("Failure in printing mechanism", DeviceStatusBitsStringType.Error),
            ("General error", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("During command some of the fields for the sums overflow", DeviceStatusBitsStringType.Error),
            ("Command cannot be performed in the current fiscal mode", DeviceStatusBitsStringType.Error),
            ("Operational memory was cleared", DeviceStatusBitsStringType.Error),
            ("Low battery (the clock is in reset state)", DeviceStatusBitsStringType.Error),
            ("RAM failure after switch ON", DeviceStatusBitsStringType.Error),
            ("Paper cover is open", DeviceStatusBitsStringType.Error),
            ("The internal terminal is not working", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("No paper", DeviceStatusBitsStringType.Error),
            ("Not enough paper", DeviceStatusBitsStringType.Warning),
            ("End of KLEN(under 1MB free)", DeviceStatusBitsStringType.Error),
            ("A fiscal receipt is opened", DeviceStatusBitsStringType.Status),
            ("Coming end of KLEN (10MB free)", DeviceStatusBitsStringType.Warning),
            ("A non-fiscal receipt is opened", DeviceStatusBitsStringType.Status),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
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

            ("Error during writing from the fiscal memory", DeviceStatusBitsStringType.Error),
            ("EIK is entered", DeviceStatusBitsStringType.Status),
            ("FM number has been set", DeviceStatusBitsStringType.Status),
            ("There is space for not more than 50 entries in the FM", DeviceStatusBitsStringType.Warning),
            ("TFiscal memory is fully engaged", DeviceStatusBitsStringType.Error),
            ("FM general error", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("The fiscal memory is in the 'read-only' mode", DeviceStatusBitsStringType.Error),
            ("The fiscal memory is formatted", DeviceStatusBitsStringType.Status),
            ("The last record in the fiscal memory is not successful", DeviceStatusBitsStringType.Error),
            ("The printer is in a fiscal mode", DeviceStatusBitsStringType.Status),
            ("Tax rates have been entered at least once", DeviceStatusBitsStringType.Status),
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
