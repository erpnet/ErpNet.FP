using System.Runtime.Serialization;

namespace ErpNet.FP.Core
{
    /// <summary>
    /// Payment type. The printer should be appropriately configured.
    /// </summary>
    public enum PaymentType
    { 
        [EnumMember(Value = "cash")]
        Cash = 0,

        [EnumMember(Value = "check")]
        Check = 1,

        [EnumMember(Value = "coupon")]
        Coupon = 2,

        [EnumMember(Value = "voucher")]
        Voucher = 3,

        [EnumMember(Value = "card")]
        Card = 7,

        [EnumMember(Value = "bank")]
        Bank = 8,

        [EnumMember(Value = "reserved1")]
        Reserved1 = 9,

        [EnumMember(Value = "reserved2")]
        Reserved2 = 10
    }
}
