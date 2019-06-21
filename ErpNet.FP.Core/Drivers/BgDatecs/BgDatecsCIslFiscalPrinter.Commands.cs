using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Datecs Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgDatecsCIslFiscalPrinter : BgIslFiscalPrinter
    {
        protected const byte
            CommandDatecsOpenReversalReceipt = 0x2e;

        public override (string, DeviceStatus) OpenReceipt(
            string uniqueSaleNumber,
            string operatorId,
            string operatorPassword)
        {
            var header = string.Join(",",
                new string[] {
                    String.IsNullOrEmpty(operatorId) ?
                        Options.ValueOrDefault("Operator.ID", "1")
                        :
                        operatorId,
                    String.IsNullOrEmpty(operatorId) ?
                        Options.ValueOrDefault("Operator.Password", "1").WithMaxLength(Info.OperatorPasswordMaxLength)
                        :
                        operatorPassword,
                    uniqueSaleNumber,
                    "1"
                });
            return Request(CommandOpenFiscalReceipt, header);
        }

        public override (string, DeviceStatus) GetTaxIdentificationNumber()
        {
            var (response, deviceStatus) = Request(CommandGetTaxIdentificationNumber);
            var commaFields = response.Split(',');
            if (commaFields.Length == 2)
            {
                return (commaFields[0].Trim(), deviceStatus);
            }
            return (string.Empty, deviceStatus);
        }

        public override string GetReversalReasonText(ReversalReason reversalReason)
        {
            switch (reversalReason)
            {
                case ReversalReason.OperatorError:
                    return "0";
                case ReversalReason.Refund:
                    return "1";
                case ReversalReason.TaxBaseReduction:
                    return "2";
                default:
                    return "0";
            }
        }

        public override (string, DeviceStatus) AddItem(
            string itemText,
            decimal unitPrice,
            TaxGroup taxGroup,
            decimal quantity = 0,
            decimal priceModifierValue = 0,
            PriceModifierType priceModifierType = PriceModifierType.None)
        // Protocol [<L1>][<Lf><L2>]<Tab><TaxCd><[Sign]Price>[*<Qwan>][,Perc|;Abs]
        {
            var itemData = new StringBuilder()
                .Append(itemText.WithMaxLength(Info.ItemTextMaxLength))
                .Append('\t').Append(GetTaxGroupText(taxGroup))
                .Append(unitPrice.ToString("F2", CultureInfo.InvariantCulture));
            if (quantity != 0)
            {
                itemData
                    .Append('*')
                    .Append(quantity.ToString(CultureInfo.InvariantCulture));
            }
            if (priceModifierType != PriceModifierType.None)
            {
                itemData
                    .Append(
                        priceModifierType == PriceModifierType.DiscountPercent
                        ||
                        priceModifierType == PriceModifierType.SurchargePercent
                        ? ',' : ';')
                    .Append((
                        priceModifierType == PriceModifierType.DiscountPercent
                        ||
                        priceModifierType == PriceModifierType.DiscountAmount
                        ? -priceModifierValue : priceModifierValue).ToString("F2", CultureInfo.InvariantCulture));
            }
            return Request(CommandFiscalReceiptSale, itemData.ToString());
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
            // Protocol: <OpCode>,<OpPwd>,<NSale>,<TillNmb>,<DocType>,<DocNumber>,<DocDateTime>,< FMNumber >[,< Invoice >,< InvNumber >,< Reason >]
            var header = string.Join(",",
                new string[] {
                    String.IsNullOrEmpty(operatorId) ?
                        Options.ValueOrDefault("Operator.ID", "1")
                        :
                        operatorId,
                    String.IsNullOrEmpty(operatorId) ?
                        Options.ValueOrDefault("Operator.Password", "1").WithMaxLength(Info.OperatorPasswordMaxLength)
                        :
                        operatorPassword,
                    uniqueSaleNumber,
                    "1",
                    GetReversalReasonText(reason),
                    receiptNumber,
                    receiptDateTime.ToString("ddMMyyHHmmss", CultureInfo.InvariantCulture),
                    fiscalMemorySerialNumber
                });

            return Request(CommandDatecsOpenReversalReceipt, header);
        }

        public override string GetPaymentTypeText(PaymentType paymentType)
        {
            switch (paymentType)
            {
                case PaymentType.Cash:
                    return "P";
                case PaymentType.Card:
                    return "C";
                case PaymentType.Reserved1:
                    return "D";
                default:
                    throw new StandardizedStatusMessageException($"Payment type {paymentType} unsupported", "E406");
            }
        }

        // 6 Bytes x 8 bits
        protected static readonly (string?, string, StatusMessageType)[] StatusBitsStrings = new (string?, string, StatusMessageType)[] {
            ("E401", "Syntax error in the received data", StatusMessageType.Error),
            ("E402", "Invalid command code received", StatusMessageType.Error),
            ("E103", "The clock is not set", StatusMessageType.Error),
            (null, "No customer display is connected", StatusMessageType.Info),
            ("E303", "Printing unit fault", StatusMessageType.Error),
            ("E199", "General error", StatusMessageType.Error),
            ("E302", "The printer cover is open", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),

            ("E403", "The command resulted in an overflow of some amount fields", StatusMessageType.Error),
            ("E404", "The command is not allowed in the current fiscal mode", StatusMessageType.Error),
            ("E104", "The RAM has been reset", StatusMessageType.Error),
            ("E102", "Low battery (the real-time clock is in RESET status)", StatusMessageType.Error),
            (null, "A refund (storno) receipt is open", StatusMessageType.Info),
            (null, "A service receipt with 90-degree rotated text printing is open", StatusMessageType.Info),
            ("E599", "The built-in tax terminal is not responding", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),

            ("E301", "No paper", StatusMessageType.Error),
            ("W301", "Low paper", StatusMessageType.Warning),
            ("E206", "End of the EJ", StatusMessageType.Error),
            (null, "A fiscal receipt is open", StatusMessageType.Info),
            ("W202", "The end of the EJ is near", StatusMessageType.Warning),
            (null, "A service receipt is open", StatusMessageType.Info),
            ("W202", "The end of the EJ is very near", StatusMessageType.Warning),
            (null, string.Empty, StatusMessageType.Reserved),

            // Byte 3, bits from 0 to 6 are SW 1 to 7
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),

            ("E202", "Fiscal memory store error", StatusMessageType.Error),
            (null, "BULSTAT UIC is set", StatusMessageType.Info),
            (null, "Unique Printer ID and Fiscal Memory ID are set", StatusMessageType.Info),
            ("W201", "There is space for less than 50 records remaining in the FP", StatusMessageType.Warning),
            ("E201", "The fiscal memory is full", StatusMessageType.Error),
            ("E299", "FM general error", StatusMessageType.Error),
            ("E304", "The printing head is overheated", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),

            ("E204", "The fiscal memory is set in READONLY mode (locked)", StatusMessageType.Error),
            (null, "The fiscal memory is formatted", StatusMessageType.Info),
            ("E202", "The last fiscal memory store operation is not successful", StatusMessageType.Error),
            (null, "The printer is in fiscal mode", StatusMessageType.Info),
            (null, "The tax rates are set at least once", StatusMessageType.Info),
            ("E203", "Fiscal memory read error", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
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
