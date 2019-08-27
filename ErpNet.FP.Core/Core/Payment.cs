using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace ErpNet.FP.Core
{
    /*
    Описание (дадено от НАП), наша константа, XML константа на НАП
    В брой, "cash", "SCash"
    С чек, "check", "SChecks"
    Талони, "coupons", "ST"
    Външни талони, "ext-coupons", "SOT"
    Амбалаж, "packaging", "SP"
    Вътрешна употреба, "internal-usage", "SSelf"
    Повреди, "damage", "SDmg"
    Кредитни/дебитни карти, "card", "SCards"
    Банкови трансфери, "bank", "SW"
    Резерв 1/НЗОК, "reserved1", "SR1"
    Резерв 2, "reserved2", "SR2
    */

    [JsonConverter(typeof(StringEnumConverter))]
    public enum PaymentType
    {
        [EnumMember(Value = "")]
        Unspecified = 0,

        // В брой, "cash", "SCash"
        [EnumMember(Value = "cash")]
        Cash = 1,

        // С чек, "check", "SChecks"
        [EnumMember(Value = "check")]
        Check = 2,

        // Талони, "coupons", "ST"
        [EnumMember(Value = "coupons")]
        Coupons = 3,

        // Външни талони, "ext-coupons", "SOT"
        [EnumMember(Value = "ext-coupons")]
        ExtCoupons = 4,

        // Амбалаж, "packaging", "SP"
        [EnumMember(Value = "packaging")]
        Packaging = 5,

        // Вътрешна употреба, "internal-usage", "SSelf"
        [EnumMember(Value = "internal-usage")]
        InternalUsage = 6,

        // Повреди, "damage", "SDmg"
        [EnumMember(Value = "damage")]
        Damage = 7,

        // Кредитни/дебитни карти, "card", "SCards"
        [EnumMember(Value = "card")]
        Card = 8,

        // Банкови трансфери, "bank", "SW"
        [EnumMember(Value = "bank")]
        Bank = 9,

        // Резерв 1/НЗОК "reserved1", "SR1"
        [EnumMember(Value = "reserved1")]
        Reserved1 = 10,

        // Резерв 2 "reserved2", "SR2"
        [EnumMember(Value = "reserved2")]
        Reserved2 = 11,

        // Служебно плащане, не се изпраща към ФУ, само за балансиране на сумата при безрестови плащания
        [EnumMember(Value = "change")]
        Change = -1
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