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
        
        protected override DeviceStatus ParseStatus(byte[]? status)
        {
            // TODO: Device status parser
            return new DeviceStatus();
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

        public override string GetPaymentTypeText(PaymentType paymentType)
        {
            switch (paymentType)
            {
                case PaymentType.Cash:
                    return "0";
                case PaymentType.BankTransfer:
                    return "1";
                case PaymentType.DebitCard:
                    return "2";
                case PaymentType.NationalHealthInsuranceFund:
                    return "3";
                case PaymentType.Voucher:
                    return "4";
                case PaymentType.Coupon:
                    return "5";
                default:
                    return "0";
            }
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

    }
}
