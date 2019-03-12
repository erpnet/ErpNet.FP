using ErpNet.FP.Core;
using GalaSoft.MvvmLight;

namespace ErpNet.FP.WpfDemo.ViewModels
{
    class PaymentLineViewModel : ObservableObject
    {
        private PaymentType type;
        public PaymentType Type
        {
            get => type;
            set => Set(ref type, value);
        }

        private string paymentName;
        public string PaymentName
        {
            get => paymentName;
            set => Set(ref paymentName, value);
        }

        private decimal amount;
        public decimal Amount
        {
            get => amount;
            set => Set(ref amount, value);
        }

        public PaymentInfoLine ToModel()
        {
            var line = new PaymentInfoLine
            {
                Type = type,
                PaymentName= paymentName,
                Amount = amount
            };
            return line;
        }
    }
}
