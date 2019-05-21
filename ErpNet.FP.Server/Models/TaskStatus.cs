
using System.Runtime.Serialization;

namespace ErpNet.FP.Server.Models
{
    public enum TaskStatus
    {
        [EnumMember(Value = "unknown")]
        Unknown,
        [EnumMember(Value = "enqueued")]
        Enqueued,
        [EnumMember(Value = "running")]
        Running,
        [EnumMember(Value = "finished")]
        Finished
    }
}
