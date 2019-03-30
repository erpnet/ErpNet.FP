using ErpNet.Fiscal.Print.Core;
using System.Text;
using System.Collections.Generic;

namespace ErpNet.Fiscal.Print.Drivers
{
    /// <summary>
    /// Fiscal printer base class for Bg printers.
    /// </summary>
    /// <seealso cref="ErpNet.Fiscal.IFiscalPrinter" />
    public abstract class BgFiscalPrinter : IFiscalPrinter
    {
        public DeviceInfo DeviceInfo => Info;

        protected IDictionary<string, string> Options { get; }
        protected IChannel Channel { get; }

        public DeviceInfo Info;

        protected Encoding PrinterEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1251);

        protected BgFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        {
            Options = new Dictionary<string, string>()
                .MergeWith(GetDefaultOptions())
                .MergeWith(options);
            Channel = channel;
        }

        public virtual IDictionary<string, string> GetDefaultOptions()
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

        public abstract bool IsReady();

        public abstract PrintInfo PrintMoneyDeposit(decimal amount);

        public abstract PrintInfo PrintMoneyWithdraw(decimal amount);

        public abstract PrintInfo PrintReceipt(Receipt receipt);

        public abstract PrintInfo PrintReversalReceipt(Receipt reversalReceipt);

        public abstract PrintInfo PrintZeroingReport();

        protected abstract DeviceStatus ParseStatus(byte[] status);

        protected virtual string WithPrinterEncoding(string text)
        {
            return PrinterEncoding.GetString(
                Encoding.Convert(Encoding.Default, PrinterEncoding, Encoding.Default.GetBytes(text)));
        }
    }
}
