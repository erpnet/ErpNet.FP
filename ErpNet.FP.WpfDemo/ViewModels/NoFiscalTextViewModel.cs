using GalaSoft.MvvmLight;

namespace ErpNet.FP.WpfDemo.ViewModels
{
    class NonFiscalTextViewModel : ObservableObject
    {
        private string text;
        public string Text
        {
            get => text;
            set => Set(ref text, value);
        }

        public string ToModel()
        {
            return text;
        }
    }
}
