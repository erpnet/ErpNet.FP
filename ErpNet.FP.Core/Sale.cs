using System;
using System.Collections.Generic;

namespace ErpNet.FP.Core
{
    /// <summary>
    /// Represents a generic Sale (invoice) that needs to be printed
    /// </summary>
    public class Sale
    {
        /// <summary>
        /// Unique sale number
        /// </summary>
        /// <remarks>
        ///   <para>According to Bulgarian legislation, each sale should be associated with a unique sale number.
        ///   This number is in the format <c>XXXXXXXX-ZZZZ-0000001</c>
        ///   </para>
        /// 
        ///   <list type="bullet">
        ///     <item>
        ///       <c>XXXXXXXX</c> - 8 symbols reseverd for the fiscal printer's unique number that is associated with this sale
        ///     </item>
        ///     <item>
        ///       <c>ZZZZ</c> - 4 symbols identifying the operator who entered the details of the sale into the system
        ///     </item>
        ///     <item>
        ///       <c>0000001</c> - 7 digits for unique sequential number for the sale. 
        ///       Sequences are unique for each individual fiscal printer
        ///     </item>
        ///   </list>
        /// </remarks>
        public string UniqueSaleNumber { get; set; }

        /// <summary>
        /// Items that are being sold. Groups price, amount and product code
        /// </summary>
        public List<SaleLine> Lines { get; }

        /// <summary>
        /// How the client paid. Includes amount and  payment type
        /// </summary>
        public List<PaymentInfoLine> PaymentInfoLines { get; }

        /// <summary>
        /// Default constructor for <see cref="Sale"/>
        /// </summary>
        public Sale()
        {
            Lines = new List<SaleLine>();
            PaymentInfoLines = new List<PaymentInfoLine>();
        }

        /// <summary>
        /// Gets the total sum that has to be paid.
        /// </summary>
        /// <returns>The total sum that has to be paid.</returns>
        public decimal GetTotalSumForPayment()
        {
            decimal totalSum = 0;

            foreach (SaleLine line in this.Lines)
            {
                totalSum += Math.Round(
                    line.Quantity * Math.Round(line.UnitPrice, 2, MidpointRounding.AwayFromZero),
                    2,
                    MidpointRounding.AwayFromZero);
            }

            return totalSum;
        }

        /// <summary>
        /// Gets the total paid sum from the payment lines.
        /// </summary>
        /// <returns>The total paid sum from the payment lines.</returns>
        public decimal GetTotalPaidSum()
        {
            decimal totalPaidAmount = 0;

            foreach (var paymentInfo in PaymentInfoLines)
            {
                totalPaidAmount += paymentInfo.Amount;
            }

            return totalPaidAmount;
        }
    }
}
