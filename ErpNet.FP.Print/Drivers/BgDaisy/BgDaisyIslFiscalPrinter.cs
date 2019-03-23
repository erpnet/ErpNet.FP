using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Print.Drivers.BgDaisy
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Daisy Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.IslFiscalPrinter" />
    public class BgDaisyIslFiscalPrinter : IFiscalPrinter
    {
        public DeviceInfo DeviceInfo => throw new System.NotImplementedException();

        public bool IsReady()
        {
            throw new System.NotImplementedException();
        }

        public PrintInfo PrintMoneyDeposit(decimal amount)
        {
            throw new System.NotImplementedException();
        }

        public PrintInfo PrintMoneyWithdraw(decimal amount)
        {
            throw new System.NotImplementedException();
        }

        public PrintInfo PrintReceipt(Receipt receipt)
        {
            throw new System.NotImplementedException();
        }

        public PrintInfo PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new System.NotImplementedException();
        }

        public PrintInfo PrintZeroingReport()
        {
            throw new System.NotImplementedException();
        }

        public void SetupPrinter()
        {
            throw new System.NotImplementedException();
        }
    }
}
