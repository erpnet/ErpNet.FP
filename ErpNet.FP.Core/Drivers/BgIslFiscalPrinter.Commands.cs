using System.Text;
using ErpNet.FP.Core;
using System.Globalization;
using System;

namespace ErpNet.FP.Core.Drivers
{
    public abstract partial class BgIslFiscalPrinter : BgFiscalPrinter
    {
        protected const byte
            CommandGetStatus = 0x4a,
            CommandGetDeviceInfo = 0x5a,
            CommandMoneyTransfer = 0x46,
            CommandOpenFiscalReceipt = 0x30,
            CommandCloseFiscalReceipt = 0x38,
            CommandAbortFiscalReceipt = 0x3c,
            CommandFiscalReceiptTotal = 0x35,
            CommandFiscalReceiptComment = 0x36,
            CommandFiscalReceiptSale = 0x31,
            CommandPrintDailyReport = 0x45;

        public virtual (string, DeviceStatus) GetStatus()
        {
            return Request(CommandGetStatus);
        }

        public virtual (string, DeviceStatus) MoneyTransfer(decimal amount)
        {
            return Request(CommandMoneyTransfer, amount.ToString("F2", CultureInfo.InvariantCulture));
        }

        public virtual (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber, string operatorID, string operatorPassword)
        {
            var header = string.Join(",",
                new string[] {
                    operatorID,
                    operatorPassword.WithMaxLength(Info.OperatorPasswordMaxLength),
                    uniqueSaleNumber
                });
            return Request(CommandOpenFiscalReceipt, header);
        }

        public virtual (string, DeviceStatus) AddItem(
            string itemText,
            decimal unitPrice,
            TaxGroup taxGroup = TaxGroup.GroupB,
            decimal quantity = 0,
            decimal priceModifierValue = 0,
            PriceModifierType priceModifierType = PriceModifierType.None)
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
                        ? ',' : '$')
                    .Append((
                        priceModifierType == PriceModifierType.DiscountPercent
                        ||
                        priceModifierType == PriceModifierType.DiscountAmount
                        ? -priceModifierValue : priceModifierValue).ToString("F2", CultureInfo.InvariantCulture));
            }
            return Request(CommandFiscalReceiptSale, itemData.ToString());
        }

        public virtual (string, DeviceStatus) AddComment(string text)
        {
            return Request(CommandFiscalReceiptComment, text.WithMaxLength(Info.CommentTextMaxLength));
        }

        public virtual (string, DeviceStatus) CloseReceipt()
        {
            return Request(CommandCloseFiscalReceipt);
        }

        public virtual (string, DeviceStatus) AbortReceipt()
        {
            return Request(CommandAbortFiscalReceipt);
        }

        public virtual (string, DeviceStatus) FullPayment()
        {
            return Request(CommandFiscalReceiptTotal);
        }

        public virtual (string, DeviceStatus) AddPayment(decimal amount, PaymentType paymentType = PaymentType.Cash)
        {
            var paymentData = new StringBuilder()
                .Append('\t')
                .Append(GetPaymentTypeText(paymentType))
                .Append(amount.ToString("F2", CultureInfo.InvariantCulture));
            return Request(CommandFiscalReceiptTotal, paymentData.ToString());
        }

        public virtual (string, DeviceStatus) PrintDailyReport()
        {
            return Request(CommandPrintDailyReport);
        }

        public virtual (string, DeviceStatus) GetRawDeviceInfo()
        {
            return Request(CommandGetDeviceInfo, "1");
        }
    }
}
