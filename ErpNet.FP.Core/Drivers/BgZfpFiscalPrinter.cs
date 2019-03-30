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
            throw new System.NotImplementedException();
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
