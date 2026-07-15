namespace ErpNet.FP.Core
{
    /// <summary>
    /// Information returned after printing a native invoice or credit note.
    /// Extends <see cref="ReceiptInfo"/> with the invoice / credit note number.
    /// </summary>
    public class InvoiceInfo : ReceiptInfo
    {
        /// <summary>
        /// The invoice / credit note number, as actually printed. A driver MUST always populate this
        /// on a successful invoice / credit note, regardless of numbering mode: echo the caller-supplied
        /// number when it is external (<see cref="NumberAssignment.ExternalOptional"/> /
        /// <see cref="NumberAssignment.ExternalRequired"/>), or read the assigned number back from the
        /// device when it is <see cref="NumberAssignment.DeviceAssigned"/> - the consumer needs it in
        /// the auto-assigned case, where it has no other way to learn the number.
        /// </summary>
        public string InvoiceNumber = string.Empty;

        public override DeviceStatusWithReceiptInfo ToDeviceStatus(DeviceStatus status)
            => new DeviceStatusWithInvoiceInfo(status, this);
    }
}
