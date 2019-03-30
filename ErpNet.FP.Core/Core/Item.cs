namespace ErpNet.FP.Core
{
    /// <summary>
    /// Represents one line in a receipt. 
    /// Can be either a comment or a fiscal line.
    /// </summary>
    public class Item
    {
        /// <summary>
        /// Gets or sets the text of the line.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        public string Text { get; set; } = "";

        /// <summary>
        /// Gets or sets a value indicating whether this line is comment.
        /// </summary>
        /// <remarks>
        /// Comment lines are printed using '#'.
        /// Comment lines contain only Text.
        /// The other attributes are not printed.
        /// </remarks>
        /// <value>
        ///   <c>true</c> if this line is comment; otherwise, <c>false</c>.
        /// </value>
        public bool IsComment { get; set; } = false;

        /// <summary>
        /// Gets or sets the tax group. 
        /// </summary>
        /// <value>
        /// The tax group.
        /// </value>
        public TaxGroup TaxGroup { get; set; } = TaxGroup.GroupB;

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        public decimal Quantity { get; set; } = 0m;

        /// <summary>
        /// Gets or sets the unit price.
        /// </summary>
        /// <value>
        /// The unit price.
        /// </value>
        public decimal UnitPrice { get; set; } = 0m;

        /// <summary>
        /// Gets or sets the discounts, surcharges.
        /// </summary>
        /// <value>
        /// The Price Modifier Value.
        /// </value>
        public decimal PriceModifierValue { get; set; } = 0m;

        /// <summary>
        /// Get or sets the PriceModifierType, None is default
        /// </summary>
        /// <value>
        /// The Price Modifier Type
        /// </value>
        /// <seealso cref="ErpNet.FP.Core.PriceModifierType"/>
        public PriceModifierType PriceModifierType { get; set; } = PriceModifierType.None;
    }
}