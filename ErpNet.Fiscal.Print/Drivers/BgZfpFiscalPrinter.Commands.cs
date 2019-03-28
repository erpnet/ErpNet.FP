using System.Text;
using ErpNet.Fiscal.Print.Core;
using System.Globalization;

namespace ErpNet.Fiscal.Print.Drivers
{
    public partial class BgZfpFiscalPrinter : BgFiscalPrinter
    {
        protected const byte
            CommandGetStatus = 0x4a,
            CommandGetDeviceInfo = 0x5a,
            CommandMoneyTransfer = 0x46,
            CommandOpenFiscalReceipt = 0x30,
            CommandCloseFiscalReceipt = 0x38,
            CommandAbortFiscalReceipt = 0x3c,
            CommandFiscalReceiptTotal = 0x35,
            CommandFiscalReceiptComment = 0x36,
            CommandFiscalReceiptSale = 0x31,
            CommandPrintDailyReport = 0x45;

        public virtual (string, DeviceStatus) MoneyTransfer(decimal amount)
        {
            return ("", null);
        }

        public virtual (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber, string operatorID, string operatorPassword)
        {
            return ("", null);
        }

        public virtual (string, DeviceStatus) AddItem(
            string itemText,
            decimal unitPrice,
            TaxGroup taxGroup = TaxGroup.GroupB,
            decimal quantity = 0,
            decimal discount = 0,
            bool isDiscountPercent = true)
        {
            return ("", null);
        }

        public virtual (string, DeviceStatus) AddComment(string text)
        {
            return ("", null);
        }

        public virtual (string, DeviceStatus) CloseReceipt()
        {
            return ("", null);
        }

        public virtual (string, DeviceStatus) AbortReceipt()
        {
            return ("", null);
        }

        public virtual (string, DeviceStatus) FullPayment()
        {
            return ("", null);
        }

        public virtual (string, DeviceStatus) AddPayment(decimal amount, PaymentType paymentType = PaymentType.Cash)
        {
            return ("", null);
        }

        public virtual (string, DeviceStatus) PrintDailyReport()
        {
            return ("", null);
        }

        public virtual (string, DeviceStatus) GetRawDeviceInfo()
        {
            return Request(0x22);
        }
    }
}
