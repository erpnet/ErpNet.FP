using System;
using System.Collections.Generic;
using System.Globalization;

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

        public override string GetPaymentTypeText(PaymentType paymentType)
        {
            switch (paymentType)
            {
                case PaymentType.Cash:
                    return "P";
                case PaymentType.Card:
                    return "L";
                case PaymentType.Check:
                    return "N";
                case PaymentType.Packaging:
                    return "I";
                case PaymentType.Reserved1:
                    return "Q";
                case PaymentType.Reserved2:
                    return "R";
                default:
                    throw new StandardizedStatusMessageException($"Payment type {paymentType} unsupported", "E406");
            }
        }

        public override (string, DeviceStatus) OpenReceipt(
            string uniqueSaleNumber,
            string operatorId,
            string operatorPassword)
        {
            var header = string.Join(",",
                new string[] {
                    String.IsNullOrEmpty(operatorId) ?
                        Options.ValueOrDefault("Operator.Name", "Operator")
                        :
                        operatorId,
                    uniqueSaleNumber
                });
            return Request(CommandOpenFiscalReceipt, header);
        }

        public override string GetReversalReasonText(ReversalReason reversalReason)
        {
            switch (reversalReason)
            {
                case ReversalReason.OperatorError:
                    return "O";
                case ReversalReason.Refund:
                    return "R";
                case ReversalReason.TaxBaseReduction:
                    return "T";
                default:
                    return "O";
            }
        }

        public override (string, DeviceStatus) OpenReversalReceipt(
            ReversalReason reason,
            string receiptNumber,
            System.DateTime receiptDateTime,
            string fiscalMemorySerialNumber,
            string uniqueSaleNumber,
            string operatorId,
            string operatorPassword)
        {
            // Protocol: <OperName>,<UNP>[,Type[ ,<FMIN>,<Reason>,<num>[,<time>[,<inv>]]]]
            var header = string.Join(",",
                new string[] {
                    String.IsNullOrEmpty(operatorId) ?
                        Options.ValueOrDefault("Operator.Name", "Operator")
                        :
                        operatorId,
                    uniqueSaleNumber,
                    "S",
                    fiscalMemorySerialNumber,
                    GetReversalReasonText(reason),
                    receiptNumber,
                    receiptDateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                });
            return Request(CommandOpenFiscalReceipt, header);
        }

        // 6 Bytes x 8 bits
        protected static readonly (string, string, StatusMessageType)[] StatusBitsStrings = new (string, string, StatusMessageType)[] {
            ("E401", "Incoming data has syntax error", StatusMessageType.Error),
            ("E402", "Code of incoming command is invalid", StatusMessageType.Error),
            ("E103", "The clock needs setting", StatusMessageType.Error),
            (string.Empty, "Not connected a customer display", StatusMessageType.Info),
            ("E303", "Failure in printing mechanism", StatusMessageType.Error),
            ("E199", "General error", StatusMessageType.Error),
            (string.Empty, string.Empty, StatusMessageType.Reserved),
            (string.Empty, string.Empty, StatusMessageType.Reserved),

            ("E403", "During command some of the fields for the sums overflow", StatusMessageType.Error),
            ("E404", "Command cannot be performed in the current fiscal mode", StatusMessageType.Error),
            ("E104", "Operational memory was cleared", StatusMessageType.Error),
            ("E102", "Low battery (the clock is in reset state)", StatusMessageType.Error),
            ("E105", "RAM failure after switch ON", StatusMessageType.Error),
            ("E302", "Paper cover is open", StatusMessageType.Error),
            ("E599", "The internal terminal is not working", StatusMessageType.Error),
            (string.Empty, string.Empty, StatusMessageType.Reserved),

            ("E301", "No paper", StatusMessageType.Error),
            ("W301", "Not enough paper", StatusMessageType.Warning),
            ("E206", "End of KLEN(under 1MB free)", StatusMessageType.Error),
            (string.Empty, "A fiscal receipt is opened", StatusMessageType.Info),
            ("W202", "Coming end of KLEN (10MB free)", StatusMessageType.Warning),
            (string.Empty, "A non-fiscal receipt is opened", StatusMessageType.Info),
            (string.Empty, string.Empty, StatusMessageType.Reserved),
            (string.Empty, string.Empty, StatusMessageType.Reserved),

            // Byte 3, bits from 0 to 6 are SW 1 to 7
            (string.Empty, string.Empty, StatusMessageType.Reserved),
            (string.Empty, string.Empty, StatusMessageType.Reserved),
            (string.Empty, string.Empty, StatusMessageType.Reserved),
            (string.Empty, string.Empty, StatusMessageType.Reserved),
            (string.Empty, string.Empty, StatusMessageType.Reserved),
            (string.Empty, string.Empty, StatusMessageType.Reserved),
            (string.Empty, string.Empty, StatusMessageType.Reserved),
            (string.Empty, string.Empty, StatusMessageType.Reserved),

            ("E202", "Error during writing to the fiscal memory", StatusMessageType.Error),
            (string.Empty, "EIK is entered", StatusMessageType.Info),
            (string.Empty, "FM number has been set", StatusMessageType.Info),
            ("W201", "There is space for not more than 50 entries in the FM", StatusMessageType.Warning),
            ("E201", "Fiscal memory is fully engaged", StatusMessageType.Error),
            ("E299", "FM general error", StatusMessageType.Error),
            (string.Empty, string.Empty, StatusMessageType.Reserved),
            (string.Empty, string.Empty, StatusMessageType.Reserved),

            ("E204", "The fiscal memory is in the 'read-only' mode", StatusMessageType.Error),
            (string.Empty, "The fiscal memory is formatted", StatusMessageType.Info),
            ("E202", "The last record in the fiscal memory is not successful", StatusMessageType.Error),
            (string.Empty, "The printer is in a fiscal mode", StatusMessageType.Info),
            (string.Empty, "Tax rates have been entered at least once", StatusMessageType.Info),
            ("E203", "Fiscal memory read error", StatusMessageType.Error),
            (string.Empty, string.Empty, StatusMessageType.Reserved),
            (string.Empty, string.Empty, StatusMessageType.Reserved)
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
                        mask >>= 1;
                        var switchState = ((mask & b) != 0) ? "ON" : "OFF";
                        switchData.Add($"SW{7 - j}={switchState}");
                    }
                    deviceStatus.AddInfo(string.Join(", ", switchData));
                }
                else
                {
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
            }
            return deviceStatus;
        }
    }
}
