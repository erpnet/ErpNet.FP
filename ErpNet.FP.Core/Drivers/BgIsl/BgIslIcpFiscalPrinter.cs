namespace ErpNet.FP.Core.Drivers.BgIcp
{
    using System.Collections.Generic;
    using ErpNet.FP.Core.Configuration;

    /// <summary>
    /// Fiscal printer using the Icp implementation of Isl Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIcpFiscalPrinter" />
    public partial class BgIslIcpFiscalPrinter : BgIcpFiscalPrinter
    {
        public BgIslIcpFiscalPrinter(
            IChannel channel, 
            ServiceOptions serviceOptions, 
            IDictionary<string, string>? options = null)
        : base(channel, serviceOptions, options) { }

        public override IDictionary<string, string>? GetDefaultOptions()
        {
            return new Dictionary<string, string>
            {
                ["Operator.ID"] = "1",
                ["Operator.Password"] = "",
            };
        }

    }
}
