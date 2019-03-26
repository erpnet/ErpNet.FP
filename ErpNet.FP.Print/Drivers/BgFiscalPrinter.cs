using ErpNet.FP.Print.Core;
using System.Text;
using System.Collections.Generic;

namespace ErpNet.FP.Print.Drivers
{
    /// <summary>
    /// Fiscal printer base class for Bg printers.
    /// </summary>
    /// <seealso cref="ErpNet.FP.IFiscalPrinter" />
    public class BgFiscalPrinter : IFiscalPrinter
    {
        public DeviceInfo DeviceInfo => FiscalPrinterInfo;

        public DeviceInfo FiscalPrinterInfo;

        protected Encoding PrinterEncoding = Encoding.GetEncoding("windows-1251");

        protected IChannel Channel;
        protected IDictionary<string, string> Options;

        public BgFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        {
            Options = options;
            Channel = channel;
        }

        public virtual bool IsReady()
        {
            throw new System.NotImplementedException();
        }

        public virtual PrintInfo PrintMoneyDeposit(decimal amount)
        {
            throw new System.NotImplementedException();
        }

        public virtual PrintInfo PrintMoneyWithdraw(decimal amount)
        {
            throw new System.NotImplementedException();
        }

        public virtual PrintInfo PrintReceipt(Receipt receipt)
        {
            throw new System.NotImplementedException();
        }

        public virtual PrintInfo PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new System.NotImplementedException();
        }

        public virtual PrintInfo PrintZeroingReport()
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetupPrinter()
        {
            throw new System.NotImplementedException();
        }

        protected virtual DeviceStatus ParseStatus(byte[] status)
        {
            throw new System.NotImplementedException();
        }

        protected virtual string EncodeTextWithPrinterEncoding(string text)
        {
            return PrinterEncoding.GetString(
                Encoding.Convert(Encoding.Default, PrinterEncoding, Encoding.Default.GetBytes(text)));
        }
    }
}
