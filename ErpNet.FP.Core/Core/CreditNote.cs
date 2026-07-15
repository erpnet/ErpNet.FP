namespace ErpNet.FP.Core
{
    /// <summary>
    /// Represents a native credit note: a reversal printed in invoice mode against an
    /// original invoice, carrying a recipient (buyer) block. Being a <see cref="ReversalReceipt"/>,
    /// it reuses the existing reversal pipeline; drivers switch to invoice mode when the
    /// reversal receipt is a <see cref="CreditNote"/>.
    /// </summary>
    public class CreditNote : ReversalReceipt, IInvoiceDocument
    {
        /// <summary>
        /// The recipient (buyer) of the credit note.
        /// </summary>
        public Recipient? Recipient { get; set; }

        /// <summary>
        /// Optional person on the buyer side who received the goods/services.
        /// Device-dependent (see <see cref="IInvoiceDocument.Receiver"/>).
        /// </summary>
        public string Receiver { get; set; } = string.Empty;

        /// <summary>
        /// Optional person on the seller side who drew up the credit note.
        /// Device-dependent (see <see cref="IInvoiceDocument.Issuer"/>).
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Caller-supplied credit note number. Its handling depends on
        /// <see cref="DeviceInfo.CreditNoteNumberAssignment"/>: required, optional, or rejected.
        /// </summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// The number of the original invoice being credited. Always required.
        /// Distinct from the inherited <see cref="ReversalReceipt.ReceiptNumber"/>, which is
        /// the original fiscal receipt number.
        /// </summary>
        public string OriginalInvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// The serial number of the fiscal device that issued the original invoice, as printed on it.
        /// Distinct from the inherited <see cref="ReversalReceipt.FiscalMemorySerialNumber"/> (the
        /// original fiscal memory number). Some devices require it to print a credit note.
        /// </summary>
        public string FiscalDeviceSerialNumber { get; set; } = string.Empty;
    }
}
