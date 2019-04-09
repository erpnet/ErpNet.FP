using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Linq;

namespace ErpNet.FP.Core
{
    /// <summary>
    /// Represents one Receipt, which can be printed on a fiscal printer.
    /// </summary>
    public class ReversalReceipt : Receipt
    {
        public string ReceiptNumber { get; set; } = string.Empty;
        public System.DateTime ReceiptDateTime { get; set; }
        public string FiscalMemorySerialNumber { get; set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        public ReversalReason Reason { get; set; } = ReversalReason.OperatorError;

        public ReversalReceipt CloneReceipt(Receipt receipt)
        {
            if (receipt.Items != null)
            {
                Items = receipt.Items.ToList();
            }
            if (receipt.Payments != null)
            {
                Payments = receipt.Payments.ToList();
            }
            UniqueSaleNumber = receipt.UniqueSaleNumber;
            return this;
        }
    }
}