namespace ErpNet.FP.Core
{
    /// <summary>
    /// Common contract for fiscal documents printed in invoice mode (a native invoice or a native credit note).
    /// </summary>
    public interface IInvoiceDocument
    {
        /// <summary>
        /// The recipient (buyer) party.
        /// </summary>
        Recipient? Recipient { get; set; }

        /// <summary>
        /// Optional person on the buyer side who received the goods/services. Device-dependent.
        /// </summary>
        string Receiver { get; set; }

        /// <summary>
        /// Optional person on the seller side who drew up the document. Device-dependent.
        /// </summary>
        string Issuer { get; set; }

        /// <summary>
        /// Optional caller-supplied document number. Honored only by devices that support
        /// external numbering; otherwise the device assigns the number itself.
        /// </summary>
        string Number { get; set; }
    }
}
