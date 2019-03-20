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
        public string Text { get; set; }

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
        public bool IsComment { get; set; }

        /// <summary>
        /// Gets or sets the tax group. The first tax group is 1.
        /// </summary>
        /// <value>
        /// The tax group.
        /// </value>
        public TaxGroup TaxGroup { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Gets or sets the unit price.
        /// </summary>
        /// <value>
        /// The unit price.
        /// </value>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Gets or sets the discount.
        /// </summary>
        /// <value>
        /// The discount.
        /// </value>
        public decimal Discount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the discount is specified as percent or absolute value.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the discount is specified as percent; <c>false</c> if it is absolute value.
        /// </value>
        public bool IsDiscountPercent { get; set; }
    }
}