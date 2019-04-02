using System.Linq;

namespace ErpNet.FP.Core
{
    /// <summary>
    /// Represents one Receipt, which can be printed on a fiscal printer.
    /// </summary>
    public class ReversalReceipt : Receipt
    {
        public string ReceiptNumber { get; set; }
        public string ReceiptDate { get; set; }
        public string ReceiptTime { get; set; }
        public string FiscalMemorySerialNumber { get; set; }

        public ReversalReason ReversalReason { get; set; } = ReversalReason.OperatorError;

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