using System.Runtime.Serialization;

namespace ErpNet.FP.Core
{
    /// <summary>
    /// Price Modifier Types
    /// </summary>
    public enum PriceModifierType
    {
        /// <summary>
        /// There is no Price Modifier, so Price Modifier Value must be 0.
        /// </summary>
        [EnumMember(Value = "none")]
        None = 0,

        /// <summary>
        /// The Price Modifier Value represents the discount in percents.
        /// </summary>
        [EnumMember(Value = "discount-percent")]
        DiscountPercent = 1,

        /// <summary>
        /// The Price Modifier Value represents the discount amount.
        /// </summary>
        [EnumMember(Value = "discount-amount")]
        DiscountAmount = 2,

        /// <summary>
        /// The Price Modifier Value represents the surcharge in percents.
        /// </summary>
        [EnumMember(Value = "surcharge-percent")]
        SurchargePercent = 3,

        /// <summary>
        /// The Price Modifier Value represents the surcharge amount.
        /// </summary>
        [EnumMember(Value = "surcharge-amount")]
        SurchargeAmount = 4
    }
}
