namespace ErpNet.FP.Core
{
    [System.Serializable]
    public class FiscalPrinterNotConstructedException : FiscalPrinterException
    {
        public FiscalPrinterNotConstructedException() { }
        public FiscalPrinterNotConstructedException(string message) : base(message) { }
        public FiscalPrinterNotConstructedException(string message, System.Exception inner) : base(message, inner) { }
        protected FiscalPrinterNotConstructedException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
