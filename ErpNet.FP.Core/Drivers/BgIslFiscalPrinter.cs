using ErpNet.FP.Core;
using System;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers
{
    /// <summary>
    /// Fiscal printer using the ISL implementation.
    /// </summary>
    /// <seealso cref="ErpNet.FP.BgFiscalPrinter" />
    public abstract partial class BgIslFiscalPrinter : BgFiscalPrinter
    {
        protected BgIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options) { }

        public override DeviceStatus CheckStatus()
        {
            var (_, status) = GetStatus();
            return status;
        }

        public override DeviceStatus PrintMoneyDeposit(decimal amount)
        {
            var (response, status) = MoneyTransfer(amount);
            System.Diagnostics.Debug.WriteLine("PrintMoneyDeposit: {0}", response);
            return status;
        }

        public override DeviceStatus PrintMoneyWithdraw(decimal amount)
        {
            if (amount < 0m)
            {
                throw new ArgumentOutOfRangeException("withdraw amount must be positive number");
            }
            var (response, status) = MoneyTransfer(-amount);
            System.Diagnostics.Debug.WriteLine("PrintMoneyWithdraw: {0}", response);
            return status;
        }

        public override (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt)
        {
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

            return (new ReceiptInfo(), new DeviceStatus());
        }

        public override DeviceStatus PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new System.NotImplementedException();
        }

        public override DeviceStatus PrintZeroingReport()
        {
            var (response, status) = PrintDailyReport();
            System.Diagnostics.Debug.WriteLine("PrintZeroingReport: {0}", response);
            // 0000,0.00,273.60,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00
            return status;
        }
    }
}
