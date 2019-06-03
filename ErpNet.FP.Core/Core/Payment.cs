using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ErpNet.FP.Core
{
    /// <summary>
    /// Represents one payment in a fiscal receipt.
    /// Receipts can contain multiple payments.
    /// </summary>
    public class Payment
    {
        /// <summary>
        /// Gets or sets the type of the payment.
        /// </summary>
        /// <value>
        /// The type of the payment.
        /// </value>
        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public PaymentType PaymentType { get; set; } = PaymentType.Unspecified;

        /// <summary>
        /// Gets or sets the amount of the payment.
        /// </summary>
        /// <value>
        /// The amount.
        /// </value>
        [JsonProperty(Required = Required.Always)]
        public decimal Amount { get; set; }
    }
}