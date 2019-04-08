using ErpNet.FP.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Datecs Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgDatecsXIslFiscalPrinter : BgIslFiscalPrinter
    {
        protected const byte
           DatecsXCommandOpenStornoDocument = 0x2b;

        public override string GetPaymentTypeText(PaymentType paymentType)
        {
            switch (paymentType)
            {
                case PaymentType.Cash:
                    return "0";
                case PaymentType.Check:
                    return "1";
                case PaymentType.Coupon:
                    return "2";
                case PaymentType.Voucher:
                    return "3";
                case PaymentType.Card:
                    return "7";
                case PaymentType.Bank:
                    return "8";
                case PaymentType.Reserved1:
                    return "9";
                case PaymentType.Reserved2:
                    return "10";
                default:
                    return "0";
            }
        }

        public override string GetTaxGroupText(TaxGroup taxGroup)
        {

            switch (taxGroup)
            {
                case TaxGroup.GroupA:
                    return "1";
                case TaxGroup.GroupB:
                    return "2";
                case TaxGroup.GroupC:
                    return "3";
                case TaxGroup.GroupD:
                    return "4";
                default:
                    return "2";
            }
        }

        public override (string, DeviceStatus) MoneyTransfer(decimal amount)
        {
            // Protocol: {Type}<SEP>{Amount}<SEP>
            return Request(CommandMoneyTransfer, string.Join("\t",
                amount<0 ? "1" : "0",
                Math.Abs(amount).ToString("F2", CultureInfo.InvariantCulture),
                ""));
        }

        public override (string, DeviceStatus) AddItem(
            string itemText,
            decimal unitPrice,
            TaxGroup taxGroup = TaxGroup.GroupB,
            decimal quantity = 0m,
            decimal priceModifierValue = 0m,
            PriceModifierType priceModifierType = PriceModifierType.None)
        {
            string PriceModifierTypeToProtocolValue()
            {
                switch (priceModifierType)
                {
                    case PriceModifierType.None:
                        return "0";
                    case PriceModifierType.DiscountPercent:
                        return "2";
                    case PriceModifierType.DiscountAmount:
                        return "4";
                    case PriceModifierType.SurchargePercent:
                        return "1";
                    case PriceModifierType.SurchargeAmount:
                        return "3";
                    default:
                        return "";
                }
            }

            // Protocol: {PluName}<SEP>{TaxCd}<SEP>{Price}<SEP>{Quantity}<SEP>{DiscountType}<SEP>{DiscountValue}<SEP>{Department}<SEP>
            var itemData = string.Join("\t",
                itemText.WithMaxLength(Info.ItemTextMaxLength),
                GetTaxGroupText(taxGroup),
                unitPrice.ToString("F2", CultureInfo.InvariantCulture),
                quantity.ToString(CultureInfo.InvariantCulture),
                PriceModifierTypeToProtocolValue(),
                priceModifierValue.ToString("F2", CultureInfo.InvariantCulture),
                "0",
                "");
            return Request(CommandFiscalReceiptSale, itemData);
        }

        public override (string, DeviceStatus) AddComment(string text)
        {
            return Request(CommandFiscalReceiptComment, text.WithMaxLength(Info.CommentTextMaxLength) + "\t");
        }
        public override (string, DeviceStatus) AddPayment(decimal amount, PaymentType paymentType = PaymentType.Cash)
        {
            // Protocol: {PaidMode}<SEP>{Amount}<SEP>{Type}<SEP>
            var paymentData = string.Join("\t",
                GetPaymentTypeText(paymentType),
                amount.ToString("F2", CultureInfo.InvariantCulture),
                "1",
                "");

            return Request(CommandFiscalReceiptTotal, paymentData);
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

        public override (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber)
        {
            var header = string.Join("\t",
                new string[] {
                    Options.ValueOrDefault("Operator.ID", "1"),
                    Options.ValueOrDefault("Operator.Password", "0000").WithMaxLength(Info.OperatorPasswordMaxLength),
                    uniqueSaleNumber,
                    "1",
                    "",
                    ""
                });
            return Request(CommandOpenFiscalReceipt, header);
        }

        public override (string, DeviceStatus) OpenReversalReceipt(
            ReversalReason reason,
            string receiptNumber,
            System.DateTime receiptDateTime,
            string fiscalMemorySerialNumber,
            string uniqueSaleNumber)
        {
            // Protocol: {OpCode}<SEP>{OpPwd}<SEP>{TillNmb}<SEP>{Storno}<SEP>{DocNum}<SEP>{DateTime}<SEP>{FMNumber}<SEP>
            //           {Invoice}<SEP>{ToInvoice}<SEP>{Reason}<SEP>{NSale}<SEP>
            var headerData = string.Join("\t",
                Options.ValueOrDefault("Operator.ID", "1"),
                Options.ValueOrDefault("Operator.Password", "0000").WithMaxLength(Info.OperatorPasswordMaxLength),
                "1",
                GetReversalReasonText(reason),
                receiptNumber,
                receiptDateTime.ToString("dd-MM-yy HH:mm:ss", CultureInfo.InvariantCulture),
                fiscalMemorySerialNumber,                
                "",
                "",
                "",
                uniqueSaleNumber,
                "");

            return Request(DatecsXCommandOpenStornoDocument, headerData.ToString());
        }

        

        public override (string, DeviceStatus) PrintDailyReport()
        {
            return Request(CommandPrintDailyReport, "Z\t");
        }

        // 8 Bytes x 8 bits
        protected enum DeviceStatusBitsStringType { Error, Warning, Status, Reserved };

        protected static readonly (string, DeviceStatusBitsStringType)[] StatusBitsStrings = new[] {
            ("Syntax error", DeviceStatusBitsStringType.Error),
            ("Command code is invalid", DeviceStatusBitsStringType.Error),
            ("The real time clock is not synchronized", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            ("Failure in printing mechanism", DeviceStatusBitsStringType.Error),
            ("General error", DeviceStatusBitsStringType.Error),
            ("Cover is open", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("Overflow during command execution", DeviceStatusBitsStringType.Error),
            ("Command is not permitted", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("End of paper", DeviceStatusBitsStringType.Error),
            ("Near paper end", DeviceStatusBitsStringType.Warning),
            ("EJ is full", DeviceStatusBitsStringType.Error),
            ("Fiscal receipt is open", DeviceStatusBitsStringType.Status),
            ("EJ nearly full", DeviceStatusBitsStringType.Warning),
            ("Nonfiscal receipt is open", DeviceStatusBitsStringType.Status),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            ("Error when trying to access data stored in the FM", DeviceStatusBitsStringType.Error),
            ("Tax number is set", DeviceStatusBitsStringType.Status),
            ("Serial number and number of FM are set", DeviceStatusBitsStringType.Status),
            ("There is space for less then 60 reports in Fiscal memory", DeviceStatusBitsStringType.Warning),
            ("FM full", DeviceStatusBitsStringType.Error),
            ("FM general error", DeviceStatusBitsStringType.Error),
            ("Fiscal memory is not found or damaged", DeviceStatusBitsStringType.Error),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            (string.Empty, DeviceStatusBitsStringType.Reserved),
            ("FM is formatted", DeviceStatusBitsStringType.Status),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            ("Device is fiscalized", DeviceStatusBitsStringType.Status),
            ("VAT are set at least once", DeviceStatusBitsStringType.Status),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),

            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
            (string.Empty, DeviceStatusBitsStringType.Reserved),
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
