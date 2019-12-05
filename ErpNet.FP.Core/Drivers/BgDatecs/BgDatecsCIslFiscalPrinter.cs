namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    using System.Collections.Generic;
    using ErpNet.FP.Core.Configuration;

    /// <summary>
    /// Fiscal printer using the ISL implementation of Datecs Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgDatecsCIslFiscalPrinter : BgIslFiscalPrinter
    {
        public BgDatecsCIslFiscalPrinter(
            IChannel channel, 
            ServiceOptions serviceOptions, 
            IDictionary<string, string>? options = null)
        : base(channel, serviceOptions, options) { }

        public override IDictionary<string, string>? GetDefaultOptions()
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
