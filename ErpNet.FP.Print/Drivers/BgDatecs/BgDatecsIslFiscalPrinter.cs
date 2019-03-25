using ErpNet.FP.Print.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Print.Drivers.BgDatecs
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Datecs Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public class BgDatecsIslFiscalPrinter : BgIslFiscalPrinter
    {
        public BgDatecsIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null) 
        : base(channel, options) {
        }

    }
}
