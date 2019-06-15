using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace ErpNet.FP.Core
{
    public enum PaymentType
    {
        [EnumMember(Value = "")]
        Unspecified = 0,
        [EnumMember(Value = "cash")]
        Cash = 1,
        [EnumMember(Value = "card")]
        Card = 2,
        [EnumMember(Value = "check")]
        Check = 3,
        [EnumMember(Value = "packaging")]
        Packaging = 4,
        [EnumMember(Value = "reserved1")]
        Reserved1 = 5,
        [EnumMember(Value = "reserved2")]
        Reserved2 = 6
    }

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