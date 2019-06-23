using System;

namespace ErpNet.FP.Core
{
    [Serializable]
    public class FiscalPrinterException : Exception
    {
        public FiscalPrinterException() { }
        public FiscalPrinterException(string message) : base(message) { }
        public FiscalPrinterException(string message, Exception inner) : base(message, inner) { }
        protected FiscalPrinterException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
