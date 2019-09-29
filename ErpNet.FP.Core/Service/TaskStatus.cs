namespace ErpNet.FP.Core.Service
{

    using System.Runtime.Serialization;

    public enum TaskStatus
    {
        [EnumMember(Value = "unknown")]
        Unknown,
        [EnumMember(Value = "enqueued")]
        Enqueued,
        [EnumMember(Value = "running")]
        Running,
        [EnumMember(Value = "finished")]
        Finished,
        [EnumMember(Value = "timeout")]
        Timeout
    }
}
