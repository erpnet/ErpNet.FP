namespace ErpNet.FP.Core
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public enum ItemType
    {
        [EnumMember(Value = "sale")]
        Sale,
        [EnumMember(Value = "comment")]
        Comment,
        [EnumMember(Value = "footer-comment")]
        FooterComment,
        [EnumMember(Value = "surcharge-amount")]
        SurchargeAmount,
        [EnumMember(Value = "discount-amount")]
        DiscountAmount
    }

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

    public enum TaxGroup
    {
        Unspecified = 0,
        TaxGroup1 = 1,
        TaxGroup2 = 2,
        TaxGroup3 = 3,
        TaxGroup4 = 4,
        TaxGroup5 = 5,
        TaxGroup6 = 6,
        TaxGroup7 = 7,
        TaxGroup8 = 8
    }

    /// <summary>
    /// Represents one line in a receipt. 
    /// Can be either a comment or a fiscal line.
    /// </summary>
    public class Item
    {
        public int ItemCode { get; set; } = 999;
        /// <summary>
        /// ItemType is the type of the item row
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType Type { get; set; } = ItemType.Sale;

        /// <summary>
        /// Gets or sets the text of the line.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        public string Text { get; set; } = "";

        /// <summary>
        /// Gets or sets the tax group. 
        /// </summary>
        /// <value>
        /// The tax group.
        /// </value>        
        public TaxGroup TaxGroup { get; set; } = TaxGroup.Unspecified;

        /// <summary>
        /// Gets or sets the department. Department 0 means no department.
        /// </summary>
        /// <value>
        /// The department.
        /// </value>
        public int Department { get; set; } = 0;

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
        /// Gets or sets the amount. Used in discount and surcharge amount types.
        /// </summary>
        /// <value>
        /// The amount of discount or the amount of surcharge
        /// </value>
        public decimal Amount { get; set; } = 0m;

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
        [JsonConverter(typeof(StringEnumConverter))]
        public PriceModifierType PriceModifierType { get; set; } = PriceModifierType.None;
    }
}