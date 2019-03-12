using ErpNet.FP.Core;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErpNet.FP.WpfDemo.ViewModels
{
    class SaleViewModel: ObservableObject
    {
        private string uniqueSaleNumber;
        public string UniqueSaleNumber
        {
            get => uniqueSaleNumber;
            set => Set(ref uniqueSaleNumber, value);
        }

        private readonly ObservableCollection<SaleLineViewModel> lines;
        public ObservableCollection<SaleLineViewModel> Lines => lines;

        private readonly ObservableCollection<NonFiscalTextViewModel> nonFiscalText;
        public ObservableCollection<NonFiscalTextViewModel> NonFiscalText => nonFiscalText;

        private readonly ObservableCollection<PaymentLineViewModel> paymentLines;
        public ObservableCollection<PaymentLineViewModel> PaymentLines => paymentLines;

        public SaleViewModel()
        {
            lines = new ObservableCollection<SaleLineViewModel>();
            nonFiscalText = new ObservableCollection<NonFiscalTextViewModel>();
            paymentLines = new ObservableCollection<PaymentLineViewModel>();
        }

        public Sale ToModel()
        {
            var sale = new Sale();
            sale.UniqueSaleNumber = uniqueSaleNumber;
            foreach (var line in lines)
            {
                sale.Lines.Add(line.ToModel());
            }
            foreach (var line in nonFiscalText)
            {
                sale.NonFiscalLines.Add(line.ToModel());
            }
            foreach (var payment in paymentLines)
            {
                sale.PaymentInfoLines.Add(payment.ToModel());
            }
            return sale;
        }
    }
}
