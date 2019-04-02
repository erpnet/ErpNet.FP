using ErpNet.FP.Core;
using System.Text;
using System.Collections.Generic;

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

        protected BgFiscalPrinter(IChannel channel, IDictionary<string, string> ?options = null)
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

        public virtual string GetTaxGroupText(TaxGroup taxGroup)
        {

            switch (taxGroup)
            {
                case TaxGroup.GroupA:
                    return "À";
                case TaxGroup.GroupB:
                    return "Á";
                case TaxGroup.GroupC:
                    return "Â";
                case TaxGroup.GroupD:
                    return "Ã";
                default:
                    return "Á";
            }
        }

        public virtual string GetPaymentTypeText(PaymentType paymentType)
        {
            switch (paymentType)
            {
                case PaymentType.Cash:
                    return "P";
                case PaymentType.BankTransfer:
                    return "N";
                case PaymentType.DebitCard:
                    return "C";
                case PaymentType.NationalHealthInsuranceFund:
                    return "D";
                case PaymentType.Voucher:
                    return "I";
                case PaymentType.Coupon:
                    return "J";
                default:
                    return "P";
            }
        }

        public virtual string GetReversalReasonText(ReversalReason reversalReason)
        {
            switch (reversalReason)
            {
                case ReversalReason.OperatorError:
                    return "0";
                case ReversalReason.GoodsReturn:
                    return "1";
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
