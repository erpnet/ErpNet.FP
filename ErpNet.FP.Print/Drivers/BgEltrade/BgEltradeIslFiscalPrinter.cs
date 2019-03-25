using ErpNet.FP.Print.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Print.Drivers.BgEltrade
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Eltrade Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public class BgEltradeIslFiscalPrinter : BgIslFiscalPrinter
    {
        public BgEltradeIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null) 
        : base(channel, options)
        {
        }

    }
}
