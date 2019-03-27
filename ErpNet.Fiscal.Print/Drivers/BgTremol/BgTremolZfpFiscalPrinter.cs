using System.Collections.Generic;
using ErpNet.Fiscal.Print.Core;

namespace ErpNet.Fiscal.Print.Drivers.BgTremol
{
    /// <summary>
    /// Fiscal printer using the Zfp implementation of Tremol Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.Fiscal.Drivers.BgZfpFiscalPrinter" />
    public class BgTremolZfpFiscalPrinter : BgZfpFiscalPrinter
    {
        public BgTremolZfpFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options)
        {

        }

    }
}
