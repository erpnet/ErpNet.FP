using ErpNet.Fiscal.Print.Core;
using System;
using System.Collections.Generic;

namespace ErpNet.Fiscal.Print.Drivers
{
    /// <summary>
    /// Fiscal printer using the ISL implementation.
    /// </summary>
    /// <seealso cref="ErpNet.Fiscal.BgFiscalPrinter" />
    public partial class BgIslFiscalPrinter : BgFiscalPrinter
    {

        public BgIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options)
        {
        }

        public override bool IsReady()
        {
            // TODO: status report and error handling

            var (response, _) = Request(CommandGetStatus);
            System.Diagnostics.Debug.WriteLine("IsReady: {0}", response);
            return true;
        }

        public override PrintInfo PrintMoneyDeposit(decimal amount)
        {
            // TODO: status report and error handling

            var (response, _) = MoneyTransfer(amount);
            System.Diagnostics.Debug.WriteLine("PrintMoneyWithdraw: {0}", response);
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
            System.Diagnostics.Debug.WriteLine("PrintMoneyWithdraw: {0}", response);
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
                FullPayment();
            }
            else
            {
                foreach (var payment in receipt.Payments)
                {
                    AddPayment(payment.Amount, payment.PaymentType);
                }
            }

            // Receipt finalization
            CloseReceipt();

            return new PrintInfo();
        }

        public override PrintInfo PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new System.NotImplementedException();
        }

        public override PrintInfo PrintZeroingReport()
        {
            // TODO: status report and error handling

            var (response, _) = PrintDailyReport();
            System.Diagnostics.Debug.WriteLine("PrintZeroingReport: {0}", response);
            // 0000,0.00,273.60,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00
            return new PrintInfo();
        }



    }
}
