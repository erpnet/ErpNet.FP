using ErpNet.FP.Core;

namespace ErpNet.FP.Win.Models
{
    public class TaskInfoResult
    {
        /// <summary>
        /// The current status of the task
        /// </summary>
        public TaskStatus Status = TaskStatus.Unknown;
        /// <summary>
        /// The result of the task 
        /// </summary>
        public object Result = new object();
    }
}
