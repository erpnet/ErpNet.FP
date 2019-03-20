namespace ErpNet.FP.Core
{
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
        public PaymentType PaymentType { get; set; }

        /// <summary>
        /// Gets or sets the amount of the payment.
        /// </summary>
        /// <value>
        /// The amount.
        /// </value>
        public decimal Amount { get; set; }
    }
}