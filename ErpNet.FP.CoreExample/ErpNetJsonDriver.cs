using System;
using System.Collections.Generic;
using ErpNet.FP.Core;

namespace ErpNet.FP.CoreExample
{
    /// <summary>
    /// Driver, which uses the ErpNet Json fiscal printing format.
    /// </summary>
    public class ErpNetJsonDriver : FiscalPrinterDriver
    {
        public override string DriverName => "json";

        public override IFiscalPrinter Connect(IChannel channel, IDictionary<string, string> options = null)
        {
            throw new NotImplementedException();
        }
    }
}
