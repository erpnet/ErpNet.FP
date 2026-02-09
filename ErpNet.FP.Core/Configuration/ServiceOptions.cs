namespace ErpNet.FP.Core.Configuration
{
    using System;
    using System.Collections.Generic;
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
        public Dictionary<string, string> PrinterOptions { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Configuration options for controlling web-based access to the service, 
    /// specifically handling CORS and browser security features.
    /// </summary>
    public class WebAccessOptions
    {
        /// <summary>
        /// Gets or sets the list of allowed origins for Cross-Origin Resource Sharing (CORS).
        /// Use "*" to allow all origins (less secure) or specific URLs like "https://myapp.myhost.com".
        /// Defaults to ["*"].
        /// </summary>
        public List<string> AllowedOrigins { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether to include the 'Access-Control-Allow-Private-Network' header.
        /// This is required by modern browsers when a public website attempts to 
        /// connect to a service on a local or private network.
        /// Defaults to false.
        /// </summary>
        public bool EnablePrivateNetwork { get; set; } = false;
    }

    /// <summary>
    /// Main service configuration options for ErpNet.FP.
    /// </summary>
    public class ServiceOptions
    {
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Gets or sets a value indicating whether to automatically detect 
        /// printers on service startup.
        /// </summary>
        public bool AutoDetect { get; set; } = true;

        /// <summary>
        /// Gets or sets the unique identifier for this server instance.
        /// </summary>
        public string ServerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the dictionary of manually configured printers.
        /// </summary>
        public Dictionary<string, PrinterConfig> Printers { get; set; } = new Dictionary<string, PrinterConfig>();

        /// <summary>
        /// Gets or sets the port used for UDP beacon broadcasting.
        /// </summary>
        public int UdpBeaconPort { get; set; } = 8001;

        /// <summary>
        /// Gets or sets the dictionary of additional printer properties, mapped by serial number.
        /// </summary>
        public Dictionary<string, PrinterProperties> PrintersProperties { get; set; } = new Dictionary<string, PrinterProperties>();
        public string ExcludePortList { get; set; } = string.Empty;
        public int DetectionTimeout { get; set; } = 30;

        /// <summary>
        /// Gets or sets the web access security settings for the service.
        /// </summary>
        public WebAccessOptions WebAccess { get; set; } = new WebAccessOptions();

        public void RemapPaymentTypes(string serialNumber, Dictionary<PaymentType, string> map) 
        {
            _rwLock.EnterReadLock();
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
            _rwLock.ExitReadLock();
        }

        public void ReconfigurePrinterConstants(DeviceInfo info)
        {
            _rwLock.EnterReadLock();
            if (!PrintersProperties.TryGetValue(info.SerialNumber, out PrinterProperties? printerProperties))
            {
                printerProperties = new PrinterProperties();
                PrintersProperties.Add(info.SerialNumber, printerProperties);
            }
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
            
            _rwLock.ExitReadLock();
        }

        public void ReconfigurePrinterOptions(DeviceInfo info)
        {
            _rwLock.EnterReadLock();
            try
            {
                if (!PrintersProperties.TryGetValue(info.SerialNumber, out PrinterProperties? printerProperties))
                {
                    printerProperties = new PrinterProperties();
                    PrintersProperties.Add(info.SerialNumber, printerProperties);
                }
                 
                if (printerProperties.PrinterOptions.TryGetValue("supportPaymentTerminal", out string supportPaymentTerminal))
                {
                    if (bool.TryParse(supportPaymentTerminal, out bool value))
                    {
                        info.SupportPaymentTerminal = value;
                    }
                }
                else
                {
                    printerProperties.PrinterOptions["supportPaymentTerminal"] = info.SupportPaymentTerminal.ToString();
                }
                if (printerProperties.PrinterOptions.TryGetValue("usePaymentTerminal", out string usePaymentTerminal))
                {
                    if (bool.TryParse(usePaymentTerminal, out bool value))
                    {
                        info.UsePaymentTerminal = value;
                    }
                }
                else
                {
                    printerProperties.PrinterOptions["usePaymentTerminal"] = info.UsePaymentTerminal.ToString();
                }
                
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }
    }
}