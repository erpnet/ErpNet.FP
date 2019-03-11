using System;

namespace ErpNet.FP.Core
{
    [Serializable]
    public class FiscalPrinterApiEndpointNotSetException : FiscalPrinterException
    {
        public FiscalPrinterApiEndpointNotSetException() { }
        public FiscalPrinterApiEndpointNotSetException(string message) : base(message) { }
        public FiscalPrinterApiEndpointNotSetException(string message, Exception inner) : base(message, inner) { }
        protected FiscalPrinterApiEndpointNotSetException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
