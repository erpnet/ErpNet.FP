using System;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgTremol
{
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

        public override string GetPaymentTypeText(PaymentType paymentType)
        {
            switch (paymentType)
            {
                case PaymentType.Cash:
                    return "0";
                case PaymentType.Card:
                    return "1";
                case PaymentType.Check:
                    return "2";
                default:
                    throw new StandardizedStatusMessageException($"Payment type {paymentType} unsupported", "E406");
            }
        }

    }
}
