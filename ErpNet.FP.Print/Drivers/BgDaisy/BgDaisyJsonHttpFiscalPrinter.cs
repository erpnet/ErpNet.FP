using System;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Print.Drivers.BgDaisy
{
    /// <summary>
    /// Implements the Bulgarian Daisy Json over http driver.
    /// </summary>
    /// <seealso cref="ErpNet.FP.IFiscalPrinter" />
    public class BgDaisyJsonHttpFiscalPrinter : IFiscalPrinter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BgDaisyJsonHttpFiscalPrinter"/> class.
        /// </summary>
        /// <param name="httpUrl">The HTTP URL.</param>
        public BgDaisyJsonHttpFiscalPrinter(string httpUrl)
        {
            //...

        }



        public DeviceInfo GetDeviceInfo()
        {
            throw new NotImplementedException();
        }

        public bool IsReady()
        {
            throw new NotImplementedException();
        }

        public PrintInfo PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new NotImplementedException();
        }

        public void SetupPrinter()
        {
            throw new NotImplementedException();
        }

        PrintInfo IFiscalPrinter.PrintMoneyDeposit(decimal amount)
        {
            throw new NotImplementedException();
        }

        PrintInfo IFiscalPrinter.PrintMoneyWithdraw(decimal amount)
        {
            throw new NotImplementedException();
        }

        PrintInfo IFiscalPrinter.PrintReceipt(Receipt receipt)
        {
            throw new NotImplementedException();
        }

        PrintInfo IFiscalPrinter.PrintZeroingReport()
        {
            throw new NotImplementedException();
        }
    }
}
