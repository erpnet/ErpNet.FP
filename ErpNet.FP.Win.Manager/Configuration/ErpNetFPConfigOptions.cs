using System.Collections.Generic;

namespace ErpNet.FP.Server.Configuration
{
    public class PrinterConfig
    {
        public string Uri { get; set; } = string.Empty;
    }

    public class ErpNetFPConfigOptions
    {
        public bool AutoDetect { get; set; } = true;
        public Dictionary<string, PrinterConfig> Printers { get; set; } = new Dictionary<string, PrinterConfig>();
    }
}
