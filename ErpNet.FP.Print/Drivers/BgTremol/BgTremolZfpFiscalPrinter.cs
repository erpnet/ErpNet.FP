using System.Collections.Generic;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Print.Drivers.BgTremol
{
    /// <summary>
    /// Fiscal printer using the Zfp implementation of Tremol Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgZfpFiscalPrinter" />
    public class BgTremolZfpFiscalPrinter : BgZfpFiscalPrinter
    {
        public BgTremolZfpFiscalPrinter(IChannel channel, IDictionary<string, string> options = null) 
        : base(channel, options) {
            FiscalPrinterInfo.Company = "Tremol";
        }

    }
}
