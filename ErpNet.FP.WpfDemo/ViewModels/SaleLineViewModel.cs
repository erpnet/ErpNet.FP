using ErpNet.FP.Core;
using GalaSoft.MvvmLight;

namespace ErpNet.FP.WpfDemo.ViewModels
{
    class SaleLineViewModel : ObservableObject
    {
        private string productName;
        public string ProductName
        {
            get => productName;
            set => Set(ref productName, value);
        }

        private decimal quantity;
        public decimal Quantity
        {
            get => quantity;
            set => Set(ref quantity, value);
        }

        private decimal unitPrice;
        public decimal UnitPrice
        {
            get => unitPrice;
            set => Set(ref unitPrice, value);
        }

        private TaxGroup taxGroup;
        public TaxGroup TaxGroup
        {
            get => taxGroup;
            set => Set(ref taxGroup, value);
        }

        public SaleLineViewModel() 
        {
            taxGroup = TaxGroup.GroupB;
        }

        public SaleLine ToModel()
        {
            var line = new SaleLine()
            {
                ProductName = productName,
                Quantity = quantity,
                TaxGroup = taxGroup,
                UnitPrice = unitPrice
            };
            return line;
        }
    }
}
