using ErpNet.FP.Core;
using System;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers
{
    /// <summary>
    /// Fiscal printer using the Zfp implementation.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Core.Drivers.BgFiscalPrinter" />
    public abstract partial class BgZfpFiscalPrinter : BgFiscalPrinter
    {
        protected BgZfpFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options) { }

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

        public override DeviceStatus CheckStatus()
        {
            var (_, status) = GetStatus();
            return status;
        }

        public override DeviceStatus PrintMoneyDeposit(decimal amount)
        {
            var (response, status) = MoneyTransfer(amount);
            System.Diagnostics.Debug.WriteLine($"PrintMoneyDeposit: {response}");
            return status;
        }

        public override DeviceStatus PrintMoneyWithdraw(decimal amount)
        {
            if (amount < 0m)
            {
                throw new ArgumentOutOfRangeException("withdraw amount must be positive number");
            }
            var (response, status) = MoneyTransfer(-amount);
            System.Diagnostics.Debug.WriteLine($"PrintMoneyWithdraw: {response}");
            return status;
        }

        public override (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt)
        {
            // Receipt header
            OpenReceipt(receipt.UniqueSaleNumber);

            // Receipt items
            foreach (var item in receipt.Items)
            {
                if (item.IsComment)
                {
                    AddComment(item.Text);
                }
                else
                {
                    if (item.PriceModifierValue < 0m)
                    {
                        throw new ArgumentOutOfRangeException("priceModifierValue amount must be positive number");
                    }
                    if (item.PriceModifierValue != 0m && item.PriceModifierType == PriceModifierType.None)
                    {
                        throw new ArgumentOutOfRangeException("priceModifierValue must be 0 if priceModifierType is None");
                    }
                    AddItem(
                        item.Text,
                        item.UnitPrice,
                        item.TaxGroup,
                        item.Quantity,
                        item.PriceModifierValue,
                        item.PriceModifierType
                    );
                }
            }

            // Receipt payments
            if (receipt.Payments == null || receipt.Payments.Count == 0)
            {
                FullPaymentAndCloseReceipt();
            }
            else
            {
                foreach (var payment in receipt.Payments)
                {
                    AddPayment(payment.Amount, payment.PaymentType);
                }
                CloseReceipt();
            }

            return (new ReceiptInfo(), new DeviceStatus());
        }

        public override DeviceStatus PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new System.NotImplementedException();
        }

        public override DeviceStatus PrintZeroingReport()
        {
            var (response, status) = PrintDailyReport();
            System.Diagnostics.Debug.WriteLine($"PrintDailyReport: {response}");
            return status;
        }
    }
}
