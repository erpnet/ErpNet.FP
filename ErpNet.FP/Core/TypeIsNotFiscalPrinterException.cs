namespace ErpNet.FP.Core
{
    [System.Serializable]
    public class TypeIsNotFiscalPrinterException : FiscalPrinterException
    {
        public TypeIsNotFiscalPrinterException() { }
        public TypeIsNotFiscalPrinterException(string message) : base(message) { }
        public TypeIsNotFiscalPrinterException(string message, System.Exception inner) : base(message, inner) { }
        protected TypeIsNotFiscalPrinterException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
