using System.Runtime.Serialization;

namespace ErpNet.FP.Core
{
    /// <summary>
    /// Price Modifier Types
    /// </summary>
    public enum PaymentType
    {
        [EnumMember(Value = "")]
        Unspecified = 0,
        [EnumMember(Value = "cash")]
        Cash = 1,
        [EnumMember(Value = "card")]
        Card = 2,
        [EnumMember(Value = "check")]
        Check = 3,
        [EnumMember(Value = "packaging")]
        Packaging = 4,
        [EnumMember(Value = "reserved1")]
        Reserved1 = 5,
        [EnumMember(Value = "reserved2")]
        Reserved2 = 6
    }
}
