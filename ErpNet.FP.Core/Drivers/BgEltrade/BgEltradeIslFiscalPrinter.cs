using ErpNet.FP.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgEltrade
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Eltrade Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgEltradeIslFiscalPrinter : BgIslFiscalPrinter
    {
        public BgEltradeIslFiscalPrinter(IChannel channel, IDictionary<string, string> ?options = null)
        : base(channel, options) { }

        public override IDictionary<string, string>? GetDefaultOptions()
        {
            return new Dictionary<string, string>
            {
                ["Operator.ID"] = "1",
                ["Operator.Name"] = "Operator",
                ["Operator.Password"] = "1",

                ["Administrator.ID"] = "20",
                ["Administrator.Password"] = "9999"
            };
        }

    }
}
