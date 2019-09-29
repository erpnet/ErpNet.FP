namespace ErpNet.FP.Core.Drivers.BgTremol
{
    using System.Collections.Generic;

    /// <summary>
    /// Fiscal printer using the Zfp V2 implementation of Tremol Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgTremolZfpV2FiscalPrinter" />
    public partial class BgTremolZfpV2FiscalPrinter : BgZfpFiscalPrinter
    {
        public BgTremolZfpV2FiscalPrinter(IChannel channel, IDictionary<string, string>? options = null)
        : base(channel, options) { }

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

        public override IDictionary<PaymentType, string> GetPaymentTypeMappings()
        {
            return new Dictionary<PaymentType, string> {
                { PaymentType.Cash,       "0" },
                { PaymentType.Card,       "1" },
                { PaymentType.Check,      "2" }
            };
        }

    }
}
