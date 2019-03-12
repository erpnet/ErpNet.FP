namespace ErpNet.FP.Core
{
    [System.Serializable]
    public class FiscalPrinterDeviceTypeMismatchException : FiscalPrinterException
    {
        public FiscalPrinterDeviceTypeMismatchException() { }
        public FiscalPrinterDeviceTypeMismatchException(string message) : base(message) { }
        public FiscalPrinterDeviceTypeMismatchException(string message, System.Exception inner) : base(message, inner) { }
        protected FiscalPrinterDeviceTypeMismatchException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
