using System;
using System.Collections.Generic;
using System.Text;

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
