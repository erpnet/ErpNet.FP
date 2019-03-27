using ErpNet.Fiscal.Print.Core;
using System.Text;
using System.Collections.Generic;

namespace ErpNet.Fiscal.Print.Drivers
{
    /// <summary>
    /// Fiscal printer base class for Bg printers.
    /// </summary>
    /// <seealso cref="ErpNet.Fiscal.IFiscalPrinter" />
    public class BgFiscalPrinter : IFiscalPrinter
    {
        public DeviceInfo DeviceInfo => Info;

        protected IDictionary<string, string> Options { get; }
        protected IChannel Channel { get; }

        public DeviceInfo Info;

        protected Encoding PrinterEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1251);

        public BgFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        {
            Options = options;
            Channel = channel;
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

        protected virtual string WithPrinterEncoding(string text)
        {
            return PrinterEncoding.GetString(
                Encoding.Convert(Encoding.Default, PrinterEncoding, Encoding.Default.GetBytes(text)));
        }
    }
}
