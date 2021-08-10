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
        /// Expresses support of payment terminal for current device model
        /// </summary>
        public bool SupportPaymentTerminal = false;
        /// <summary>
        /// Expresses using of payment terminal for current device
        /// </summary>
        public bool UsePaymentTerminal = false;
    }
}