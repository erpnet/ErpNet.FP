using System.Text;
using ErpNet.Fiscal.Print.Core;
using System.Globalization;

namespace ErpNet.Fiscal.Print.Drivers
{
    public partial class BgZfpFiscalPrinter : BgFiscalPrinter
    {
        protected const byte
            CommandReadFDNumbers = 0x60,
            CommandVersion = 0x21,
            CommandPrintDailyFiscalReport = 0x7c,
            CommandNoFiscalRAorPOAmount = 0x3b,
            CommandGSCommand = 0x1d;

        public virtual (string, DeviceStatus) MoneyTransfer(decimal amount)
        {
            return Request(CommandNoFiscalRAorPOAmount, string.Join(";", new string[]
            {
                Options["Operator.ID"],
                Options["Operator.Password"],
                "0", // Protocol: Reserved 
                amount.ToString("F2", CultureInfo.InvariantCulture)
            }));
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

        public virtual (string, DeviceStatus) AddPayment(
            decimal amount, 
            PaymentType paymentType = PaymentType.Cash)
        {
            return ("", null);
        }

        public virtual (string, DeviceStatus) PrintDailyReport()
        {
            return Request(CommandPrintDailyFiscalReport, "Z");
        }

        public virtual (string, DeviceStatus) GetRawDeviceInfo()
        {
            var (responseFD, _) = Request(CommandReadFDNumbers);
            var (responseV, deviceStatus) = Request(CommandVersion);
            return (responseV + responseFD, deviceStatus);
        }
    }
}
