namespace ErpNet.FP.Core
{
    /// <summary>
    /// Represents a native fiscal invoice: a fiscal receipt printed in invoice mode,
    /// carrying a recipient (buyer) block. Being a <see cref="Receipt"/>, it reuses the
    /// existing print/validate pipeline; drivers switch to invoice mode when the receipt
    /// is an <see cref="Invoice"/>.
    /// </summary>
    public class Invoice : Receipt, IInvoiceDocument
    {
        /// <summary>
        /// The recipient (buyer) of the invoice.
        /// </summary>
        public Recipient? Recipient { get; set; }

        /// <summary>
        /// Optional person on the buyer side who received the goods/services. Device-dependent.
        /// </summary>
        public string Receiver { get; set; } = string.Empty;

        /// <summary>
        /// Optional person on the seller side who drew up the invoice. Device-dependent.
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Caller-supplied invoice number. Its handling depends on
        /// <see cref="DeviceInfo.InvoiceNumberAssignment"/>: required, optional, or rejected
        /// (when the device assigns the number itself).
        /// </summary>
        public string Number { get; set; } = string.Empty;
    }
}
