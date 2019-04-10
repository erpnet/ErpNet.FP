using System.Collections.Generic;
using System.Text;

namespace ErpNet.FP.Core.Drivers
{
    /// <summary>
    /// Fiscal printer base class for Bg printers.
    /// </summary>
    /// <seealso cref="ErpNet.FP.IFiscalPrinter" />
    public abstract class BgFiscalPrinter : IFiscalPrinter
    {
        public DeviceInfo DeviceInfo => Info;
        protected IDictionary<string, string> Options { get; }
        protected IChannel Channel { get; }

        public DeviceInfo Info = new DeviceInfo();

        protected Encoding PrinterEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1251);

        protected enum DeviceStatusBitsStringType { Error, Warning, Status, Reserved };

        protected BgFiscalPrinter(IChannel channel, IDictionary<string, string>? options = null)
        {
            Options = new Dictionary<string, string>()
                .MergeWith(GetDefaultOptions())
                .MergeWith(options);
            Channel = channel;
        }

        public virtual IDictionary<string, string>? GetDefaultOptions()
        {
            return null;
        }

        public virtual string GetTaxGroupText(string taxGroup)
        {

            switch (taxGroup)
            {
                case "":
                    return "Á";
                default:
                    return taxGroup;
            }
        }

        public virtual string GetPaymentTypeText(string paymentType)
        {
            switch (paymentType)
            {
                case "":
                    return "P";
                case "cash":
                    return "P";
                case "card":
                    return "C";
                default:
                    return paymentType;
            }
        }

        public virtual string GetReversalReasonText(ReversalReason reversalReason)
        {
            switch (reversalReason)
            {
                case ReversalReason.OperatorError:
                    return "0";
                case ReversalReason.Refund:
                    return "1";
                case ReversalReason.TaxBaseReduction:
                    return "2";
                default:
                    return "0";
            }
        }

        public abstract DeviceStatus CheckStatus();

        public abstract DeviceStatus PrintMoneyDeposit(decimal amount);

        public abstract DeviceStatus PrintMoneyWithdraw(decimal amount);

        public abstract (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt);

        public abstract DeviceStatus PrintReversalReceipt(ReversalReceipt reversalReceipt);

        public abstract DeviceStatus PrintZeroingReport();

        protected abstract DeviceStatus ParseStatus(byte[]? status);

        protected virtual string WithPrinterEncoding(string text)
        {
            return PrinterEncoding.GetString(
                Encoding.Convert(Encoding.Default, PrinterEncoding, Encoding.Default.GetBytes(text)));
        }
    }
}
