using System.Collections.Generic;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Print.Drivers
{
    /// <summary>
    /// Fiscal printer using the Zfp implementation.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Print.Drivers.BgFiscalPrinter" />
    public class BgZfpFiscalPrinter : BgFiscalPrinter
    {
        public BgZfpFiscalPrinter(IChannel channel, IDictionary<string, string> options = null) 
        : base (channel, options) {
        }   

        public override bool IsReady()
        {
            throw new System.NotImplementedException();
        }

        public override PrintInfo PrintMoneyDeposit(decimal amount)
        {
            throw new System.NotImplementedException();
        }

        public override PrintInfo PrintMoneyWithdraw(decimal amount)
        {
            throw new System.NotImplementedException();
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
            throw new System.NotImplementedException();
        }

        public override void SetupPrinter()
        {
            // Nothing to be configured for now.
        }
    }
}
