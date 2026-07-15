namespace ErpNet.FP.Core
{
    using System.Collections.Generic;

    public class DeviceInfo
    {
        /// <summary>
        /// Fiscal printer Uri 
        /// </summary>
        public string Uri = string.Empty;
        /// <summary>
        /// Fiscal printer serial number
        /// </summary>
        public string SerialNumber = string.Empty;
        /// <summary>
        /// Fiscal printer memory serial number
        /// </summary>
        public string FiscalMemorySerialNumber = string.Empty;
        /// <summary>
        /// Manufacturer - Company or Trademark of Company that produces the fiscal device
        /// </summary>
        public string Manufacturer = string.Empty;
        /// <summary>
        /// Model
        /// </summary>
        public string Model = string.Empty;
        /// <summary>
        /// Optional. Firmware version.
        /// </summary>
        public string FirmwareVersion = string.Empty;
        // <summary>
        /// Maximum symbols for operator names, item names, department names allowed.
        /// </summary>
        public int ItemTextMaxLength;
        /// <summary>
        /// Maximum symbols for payment names allowed.
        /// </summary>
        public int CommentTextMaxLength;
        /// <summary>
        /// Maximal operator password length allowed;
        /// </summary>
        public int OperatorPasswordMaxLength;
        /// <summary>
        /// Tax Number is Fiscal Subject Identification Number
        /// </summary>
        public string TaxIdentificationNumber = string.Empty;
        /// <summary>
        /// List of supported payment types by the device
        /// </summary>        
        public ICollection<PaymentType> SupportedPaymentTypes = new PaymentType[] { };
        /// <summary>
        /// Expresses support of item types discount-amount and surcharge-amount by the device
        /// </summary>
        public bool SupportsSubTotalAmountModifiers = false;
        /// <summary>
        /// When <see cref="SupportsSubTotalAmountModifiers"/> is true, indicates whether the device also
        /// requires a taxGroup (VAT category) on each subtotal amount modifier item
        /// (discount-amount / surcharge-amount). Optional for the caller unless this is true.
        /// </summary>
        public bool SubTotalAmountModifiersRequireTaxGroup = false;
        /// <summary>
        /// Expresses support of printing a native fiscal invoice (a receipt in invoice mode
        /// with a recipient block) by the device.
        /// </summary>
        public bool SupportsInvoice = false;
        /// <summary>
        /// Expresses support of printing a native credit note (a reversal in invoice mode
        /// against an original invoice) by the device.
        /// </summary>
        public bool SupportsCreditNote = false;
        /// <summary>
        /// Expresses how the invoice number is assigned: by the device, optionally by the caller,
        /// or required from the caller (see <see cref="NumberAssignment"/>).
        /// </summary>
        public NumberAssignment InvoiceNumberAssignment = NumberAssignment.DeviceAssigned;
        /// <summary>
        /// Expresses how the credit note number is assigned: by the device, optionally by the caller,
        /// or required from the caller (see <see cref="NumberAssignment"/>).
        /// </summary>
        public NumberAssignment CreditNoteNumberAssignment = NumberAssignment.DeviceAssigned;
        /// <summary>
        /// Expresses support of payment terminal for current device model
        /// </summary>
        public bool SupportPaymentTerminal = false;
        /// <summary>
        /// Expresses using of payment terminal for current device
        /// </summary>
        public bool UsePaymentTerminal = false;
    }
}