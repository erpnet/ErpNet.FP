using System;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Print.Drivers
{
    /// <summary>
    /// Base class for fiscal printers, which are based on the ISL (Cisco Inter-Switch Link) protocol.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Core.IFiscalPrinter" />
    public class IslFiscalPrinter : IFiscalPrinter
    {
        public virtual DeviceInfo GetDeviceInfo()
        {
            throw new NotImplementedException();
        }

        public virtual bool IsReady()
        {
            throw new NotImplementedException();
        }

        public virtual PrintInfo PrintMoneyDeposit(decimal amount)
        {
            throw new NotImplementedException();
        }

        public virtual PrintInfo PrintMoneyWithdraw(decimal amount)
        {
            throw new NotImplementedException();
        }

        public virtual PrintInfo PrintReceipt(Receipt receipt)
        {
            throw new NotImplementedException();
        }

        public virtual PrintInfo PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new NotImplementedException();
        }

        public virtual PrintInfo PrintZeroingReport()
        {
            throw new NotImplementedException();
        }

        public virtual void SetupPrinter()
        {
            throw new NotImplementedException();
        }
    }
}
