namespace ErpNet.FP.Core.Drivers.BgTremol
{
    using System.Collections.Generic;
    using ErpNet.FP.Core.Configuration;

    /// <summary>
    /// Fiscal printer using the Zfp implementation of Tremol Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgZfpFiscalPrinter" />
    public partial class BgTremolZfpFiscalPrinter : BgZfpFiscalPrinter
    {
        public BgTremolZfpFiscalPrinter(
            IChannel channel, 
            ServiceOptions serviceOptions, 
            IDictionary<string, string>? options = null)
        : base(channel, serviceOptions, options) { }

        public override IDictionary<string, string>? GetDefaultOptions()
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
