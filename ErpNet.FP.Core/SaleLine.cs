using System;
using System.Collections.Generic;
using System.Text;

namespace ErpNet.FP.Core
{
    /// <summary>
    /// Represents an item that is sold. Captures product, amount, price and tax group.
    /// </summary>
    /// <seealso cref="Sale"/>
    public class SaleLine
    {
        /// <summary>
        /// Unique code for the product being sold. Required.
        /// </summary>
        public string ProductNumber { get; set; }

        /// <summary>
        /// Descriptive name for the product being sold. Optional.
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Amount/quantity sold. Required.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Price for the product if quantity were 1
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Tax group (tax bracket) for the sale.
        /// </summary>
        public TaxGroup TaxGroup { get; set; }

        /// <summary>
        /// Default constructor for <see cref="SaleLine"/>
        /// </summary>
        public SaleLine()
        { }

        /// <summary>
        /// Constructs a new <see cref="SaleLine"/> with prepared parameters
        /// </summary>
        /// <param name="productNumber">Unique product code. Required.</param>
        /// <param name="productName">Descriptive name for the product. Required.</param>
        /// <param name="quantity"></param>
        /// <param name="unitPrice"></param>
        public SaleLine(string productNumber, string productName, decimal quantity, decimal unitPrice)
        {
            ProductNumber = productNumber;
            ProductName = productName;
            Quantity = quantity;
            UnitPrice = unitPrice;
            TaxGroup = TaxGroup.GroupB;
        }
    }
}
