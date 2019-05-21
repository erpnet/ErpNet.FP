using ErpNet.FP.Core;

namespace ErpNet.FP.Server.Models
{
    public class PrintReceiptResult
    {
        public DeviceStatus Status = new DeviceStatus();
        public ReceiptInfo Info = new ReceiptInfo();
    }
}
