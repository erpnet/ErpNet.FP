using System.Collections.Generic;

namespace ErpNet.FP.Core.Configuration {
    public class PrinterConfig {
        public string Uri { get; set; } = string.Empty;
    }

    public class ServiceOptions {
        public bool AutoDetect { get; set; } = true;
        public string ServerId { get; set; } = string.Empty;
        public Dictionary<string, PrinterConfig> Printers { get; set; } = new Dictionary<string, PrinterConfig> ();
    }
}