namespace ErpNet.FP.Core.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Newtonsoft.Json;

    public class PrinterConfig
    {
        public string Uri { get; set; } = string.Empty;
    }

    public class PrinterConfigWithId : PrinterConfig
    {
        public string Id { get; set; } = string.Empty;
    }

    public class PrinterProperties
    {
        public Dictionary<string, string> PaymentTypeMappings { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> PrinterConstants { get; set; } = new Dictionary<string, string>();
    }

    public class ServiceOptions
    {
        
        public bool AutoDetect { get; set; } = true;
        public string ServerId { get; set; } = string.Empty;
        public Dictionary<string, PrinterConfig> Printers { get; set; } = new Dictionary<string, PrinterConfig>();
        public int UdpBeaconPort { get; set; } = 8001;
        public Dictionary<string, PrinterProperties> PrintersProperties { get; set; } = new Dictionary<string, PrinterProperties>();

        private readonly ReaderWriterLockSlim RWLock = new ReaderWriterLockSlim();

        public void RemapPaymentTypes(string serialNumber, Dictionary<PaymentType, string> map) 
        {
            RWLock.EnterReadLock();
            if (PrintersProperties.TryGetValue(serialNumber, out PrinterProperties? printerProperties))
            {
                foreach (PaymentType pmt in (PaymentType[])Enum.GetValues(typeof(PaymentType)))
                {
                    var serializedKey = JsonConvert.SerializeObject(pmt).Trim('"');
                    if (printerProperties.PaymentTypeMappings.TryGetValue(serializedKey, out string newValue))
                    {
                        if (!string.IsNullOrEmpty(newValue)) {
                            map[pmt] = newValue;
                        }
                    }
                }
            }
            RWLock.ExitReadLock();
        }

        public void ReconfigurePrinterConstants(DeviceInfo info)
        {
            RWLock.EnterReadLock();
            if (PrintersProperties.TryGetValue(info.SerialNumber, out PrinterProperties? printerProperties))
            {
                if (printerProperties.PrinterConstants.TryGetValue("commentTextMaxLength", out string commentTextMaxLength)) {
                    if (int.TryParse(commentTextMaxLength, out int value)) {
                        if (value > 0)
                        {
                            info.CommentTextMaxLength = value;
                        }
                    }
                }
                if (printerProperties.PrinterConstants.TryGetValue("itemTextMaxLength", out string itemTextMaxLength))
                {
                    if (int.TryParse(itemTextMaxLength, out int value))
                    {
                        if (value > 0)
                        {
                            info.ItemTextMaxLength = value;
                        }
                    }
                }
            }
            RWLock.ExitReadLock();
        }
    }
}