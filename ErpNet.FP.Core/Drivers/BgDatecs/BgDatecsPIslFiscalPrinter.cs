using ErpNet.FP.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Datecs Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgDatecsPIslFiscalPrinter : BgIslFiscalPrinter
    {
        public BgDatecsPIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options) { }

        public override IDictionary<string, string> GetDefaultOptions()
        {
            return new Dictionary<string, string>
            {
                ["Operator.ID"] = "1",
                ["Operator.Password"] = "0000",

                ["Administrator.ID"] = "20",
                ["Administrator.Password"] = "9999"
            };
        }
    }
}
