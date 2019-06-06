using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgIncotex
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Daisy Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgIncotexIslFiscalPrinter : BgIslFiscalPrinter
    {
        public BgIncotexIslFiscalPrinter(IChannel channel, IDictionary<string, string>? options = null)
        : base(channel, options) { }

        public override IDictionary<string, string>? GetDefaultOptions()
        {
            return new Dictionary<string, string>
            {
                ["Operator.ID"] = "1",
                ["Operator.Password"] = "0"
            };
        }
    }
}
