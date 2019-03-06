namespace ErpNet.FP.Core
{
    /// <summary>
    /// Determines payment 
    /// </summary>
    /// <seealso cref="Sale"/>
    /// <seealso cref="SaleLine"/>
    public class PaymentInfoLine
    {
        /// <summary>
        /// Required. Payment type. Individual fiscal printers may need to be programmed to match the values in
        /// <see cref="PaymentType"/> enumeration
        /// </summary>
        public PaymentType Type { get; set; }

        /// <summary>
        /// Optional. What should be printed as payment method on the invoice. 
        /// Not all devices support this without being programmed first
        /// </summary>
        public string PaymentName { get; set; }

        /// <summary>
        /// Required. Amount paid by the customer, including the change.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Default constructor for <see cref="PaymentInfoLine"/>
        /// </summary>
        public PaymentInfoLine()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentInfoLine"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="paymentName">Optional payment name</param>
        public PaymentInfoLine(decimal amount, PaymentType type, string paymentName = "")
        {
            Type = type;
            Amount = amount;
            PaymentName = paymentName;
        }
    }
}
