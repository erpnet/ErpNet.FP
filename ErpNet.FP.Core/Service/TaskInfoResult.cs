using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ErpNet.FP.Core.Service
{
    public class TaskInfoResult
    {
        /// <summary>
        /// The current status of the task
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public TaskStatus TaskStatus = TaskStatus.Unknown;
        /// <summary>
        /// The result of the task 
        /// </summary>
        public object Result = new object();
    }
}
