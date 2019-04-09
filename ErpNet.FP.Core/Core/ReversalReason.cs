using System.Runtime.Serialization;

namespace ErpNet.FP.Core
{
    /// <summary>
    /// Reversal Reason
    /// </summary>
    public enum ReversalReason
    {
        [EnumMember(Value = "operator-error")]
        OperatorError = 1,
        [EnumMember(Value = "refund")]
        Refund = 2,
        [EnumMember(Value = "taxbase-reduction")]
        TaxBaseReduction = 3
    }
}
