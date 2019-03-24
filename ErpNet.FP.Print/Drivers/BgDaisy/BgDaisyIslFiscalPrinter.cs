using System.Collections.Generic;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Print.Drivers.BgDaisy
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Daisy Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public class BgDaisyIslFiscalPrinter : BgIslFiscalPrinter
    {
        public BgDaisyIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null) 
        : base(channel, options) {
        }

    }
}
