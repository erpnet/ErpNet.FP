using System.Collections.Generic;

namespace ErpNet.FP.Core
{
    public class DeviceStatus
    {
        public System.DateTime DateTime { get; set; }
        public ICollection<string> Statuses { get; } = new List<string>();
        public ICollection<string> Warnings { get; } = new List<string>();
        public ICollection<string> Errors { get; } = new List<string>();

        public bool Ok => Errors.Count == 0;
    }

}
