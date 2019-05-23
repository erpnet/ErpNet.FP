using System.Collections.Generic;

namespace ErpNet.FP.Server
{
    public class PrinterConfig
    {
        public string Uri { get; set; } = string.Empty;
    }

    public class ServerConfigOptions
    {
        public bool AutoDetect { get; set; } = true;
        public Dictionary<string, PrinterConfig> Printers { get; set; } = new Dictionary<string, PrinterConfig>();
    }
}
