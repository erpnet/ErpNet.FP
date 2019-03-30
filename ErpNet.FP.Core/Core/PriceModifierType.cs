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
        None,

        /// <summary>
        /// The Price Modifier Value represents the discount in percents.
        /// </summary>
        DiscountPercent,

        /// <summary>
        /// The Price Modifier Value represents the discount amount.
        /// </summary>
        DiscountAmount,

        /// <summary>
        /// The Price Modifier Value represents the surcharge in percents.
        /// </summary>
        SurchargePercent,

        /// <summary>
        /// The Price Modifier Value represents the surcharge amount.
        /// </summary>
        SurchargeAmount
    }
}
