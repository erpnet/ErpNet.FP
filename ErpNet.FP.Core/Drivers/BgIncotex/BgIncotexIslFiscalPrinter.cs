namespace ErpNet.FP.Core.Drivers.BgIncotex
{
    using System.Collections.Generic;
    using ErpNet.FP.Core.Configuration;

    /// <summary>
    /// Fiscal printer using the ISL implementation of Incotex.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgIncotexIslFiscalPrinter : BgIslFiscalPrinter
    {
        public BgIncotexIslFiscalPrinter(
            IChannel channel, 
            ServiceOptions serviceOptions, 
            IDictionary<string, string>? options = null)
        : base(channel, serviceOptions, options) { }

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
