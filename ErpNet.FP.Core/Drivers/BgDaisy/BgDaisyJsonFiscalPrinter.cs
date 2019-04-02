using System;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgDaisy
{
    /// <summary>
    /// Implements the Bulgarian Daisy Json driver.
    /// </summary>
    /// <seealso cref="ErpNet.FP.IFiscalPrinter" />
    public class BgDaisyJsonFiscalPrinter : IFiscalPrinter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BgDaisyJsonFiscalPrinter"/> class.
        /// </summary>
        public BgDaisyJsonFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        {
            //...

        }

        public void MergeOptionsWith(IDictionary<string, string> newOptions)
        {
            throw new NotImplementedException();
        }

        public DeviceInfo DeviceInfo => throw new NotImplementedException();

        public DeviceStatus CheckStatus()
        {
            throw new NotImplementedException();
        }

        public DeviceStatus PrintMoneyDeposit(decimal amount)
        {
            throw new NotImplementedException();
        }

        public DeviceStatus PrintMoneyWithdraw(decimal amount)
        {
            throw new NotImplementedException();
        }

        public (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt)
        {
            throw new NotImplementedException();
        }

        public DeviceStatus PrintReversalReceipt(ReversalReceipt reversalReceipt)
        {
            throw new NotImplementedException();
        }

        public DeviceStatus PrintZeroingReport()
        {
            throw new NotImplementedException();
        }

        public void SetupPrinter()
        {
            throw new NotImplementedException();
        }
    }
}
