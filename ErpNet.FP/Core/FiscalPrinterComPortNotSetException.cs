using System;

namespace ErpNet.FP.Core
{
    [Serializable]
    public class FiscalPrinterComPortNotSetException : FiscalPrinterException
    {
        public FiscalPrinterComPortNotSetException() { }
        public FiscalPrinterComPortNotSetException(string message) : base(message) { }
        public FiscalPrinterComPortNotSetException(string message, Exception inner) : base(message, inner) { }
        protected FiscalPrinterComPortNotSetException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
