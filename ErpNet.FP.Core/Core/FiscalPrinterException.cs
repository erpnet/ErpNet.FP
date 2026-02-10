namespace ErpNet.FP.Core
{
    using System;

    public class FiscalPrinterException : Exception
    {
        public FiscalPrinterException() { }

        public FiscalPrinterException(string message) : base(message) { }

        public FiscalPrinterException(string message, Exception inner) : base(message, inner) { }
    }
}
