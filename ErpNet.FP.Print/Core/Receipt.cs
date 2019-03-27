using System.Collections.Generic;

namespace ErpNet.Fiscal.Print.Core
{
    /// <summary>
    /// Represents one Receipt, which can be printed on a fiscal printer.
    /// </summary>
    public class Receipt
    {
        /// <summary>
        /// The unique sale number is a fiscally controlled number.
        /// </summary>
        public string UniqueSaleNumber { get; set; }

        /// <summary>
        /// The line items of the receipt.
        /// </summary>
        public IList<Item> Items { get; set; }

        /// <summary>
        /// The payments of the receipt. 
        /// The total amount should match the total amount of the line items.
        /// </summary>
        public IList<Payment> Payments { get; set; }

        /// <summary>
        /// Gets or sets the original fiscal memory position.
        /// Used only for reversal receipts.
        /// </summary>
        /// <value>
        /// The original fiscal memory position.
        /// </value>
        public string OriginalFiscalMemoryPosition { get; set; }
    }
}