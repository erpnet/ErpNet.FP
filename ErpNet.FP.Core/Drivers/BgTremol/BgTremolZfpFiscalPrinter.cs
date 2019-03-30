using System.Collections.Generic;
using ErpNet.FP.Core;

namespace ErpNet.FP.Core.Drivers.BgTremol
{
    /// <summary>
    /// Fiscal printer using the Zfp implementation of Tremol Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgZfpFiscalPrinter" />
    public class BgTremolZfpFiscalPrinter : BgZfpFiscalPrinter
    {
        public BgTremolZfpFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options)
        {

        }
        public override IDictionary<string, string> GetDefaultOptions()
        {
            return new Dictionary<string, string>
            {
                ["Operator.ID"] = "1",
                ["Operator.Password"] = "1",

                ["Administrator.ID"] = "20",
                ["Administrator.Password"] = "9999"
            };
        }

    }
}
