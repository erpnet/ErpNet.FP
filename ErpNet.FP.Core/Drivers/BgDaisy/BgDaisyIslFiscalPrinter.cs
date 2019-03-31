using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgDaisy
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Daisy Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgDaisyIslFiscalPrinter : BgIslFiscalPrinter
    {
        public BgDaisyIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options) { }

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
