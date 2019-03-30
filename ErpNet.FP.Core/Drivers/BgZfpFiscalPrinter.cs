using ErpNet.FP.Core;
using System;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers
{
    /// <summary>
    /// Fiscal printer using the Zfp implementation.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Core.Drivers.BgFiscalPrinter" />
    public partial class BgZfpFiscalPrinter : BgFiscalPrinter
    {
        public BgZfpFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options) {}

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

        public override bool IsReady()
        {
            throw new System.NotImplementedException();
        }

        public override PrintInfo PrintMoneyDeposit(decimal amount)
        {
            // TODO: status report and error handling

            var (response, _) = MoneyTransfer(amount);
            System.Diagnostics.Debug.WriteLine($"PrintMoneyWithdraw: {response}");
            return new PrintInfo();
        }

        public override PrintInfo PrintMoneyWithdraw(decimal amount)
        {
            // TODO: status report and error handling

            if (amount < 0m)
            {
                throw new ArgumentOutOfRangeException("withdraw amount must be positive number");
            }
            var (response, _) = MoneyTransfer(-amount);
            System.Diagnostics.Debug.WriteLine($"PrintMoneyWithdraw: {response}");
            return new PrintInfo();
        }

        public override PrintInfo PrintReceipt(Receipt receipt)
        {
            // TODO: status report and error handling

            // Receipt header
            OpenReceipt(receipt.UniqueSaleNumber, Options["Operator.ID"], Options["Operator.Password"]);

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

            return new PrintInfo();
        }

        public override PrintInfo PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new System.NotImplementedException();
        }

        public override PrintInfo PrintZeroingReport()
        {
            var (response, _) = PrintDailyReport();
            System.Diagnostics.Debug.WriteLine($"PrintDailyReport: {response}");
            return new PrintInfo();
        }
    }
}
