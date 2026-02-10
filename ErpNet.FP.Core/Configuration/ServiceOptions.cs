namespace ErpNet.FP.Core.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the base configuration for a fiscal printer connection.
    /// </summary>
    public class PrinterConfig
    {
        /// <summary>
        /// Gets or sets the URI used to connect to the printer (e.g., COM port or network address).
        /// </summary>
        public string Uri { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a printer configuration that includes a unique identifier.
    /// </summary>
    public class PrinterConfigWithId : PrinterConfig
    {
        /// <summary>
        /// Gets or sets the unique identifier for this printer configuration.
        /// </summary>
        public string Id { get; set; } = string.Empty;
    }

    /// <summary>
    /// Contains metadata and operational parameters for a specific fiscal printer.
    /// </summary>
    public class PrinterProperties
    {
        /// <summary>
        /// Gets or sets a dictionary of payment type mappings, where the key is the system payment type 
        /// and the value is the printer-specific payment identifier.
        /// </summary>
        public Dictionary<string, string> PaymentTypeMappings { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets hardware-specific constants for the printer, such as text length limits.
        /// </summary>
        public Dictionary<string, string> PrinterConstants { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets various operational options specific to the printer model.
        /// </summary>
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
        public List<string> AllowedOrigins { get; set; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether to include the 'Access-Control-Allow-Private-Network' header.
        /// This is required by modern browsers when a public website attempts to 
        /// connect to a service on a local or private network.
        /// Defaults to false.
        /// </summary>
        public bool EnablePrivateNetwork { get; set; } = false;
    }

    /// <summary>
    /// The root configuration class for the ErpNet.FP service, containing general settings 
    /// and collections of printer-specific configurations.
    /// </summary>
    public class ServiceOptions
    {
        private readonly ReaderWriterLockSlim _rwLock = new();

        /// <summary>
        /// Gets or sets a value indicating whether the service should automatically detect connected printers.
        /// </summary>
        public bool AutoDetect { get; set; } = true;

        /// <summary>
        /// Gets or sets the unique server identifier.
        /// </summary>
        public string ServerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of active printer configurations indexed by name.
        /// </summary>
        public Dictionary<string, PrinterConfig> Printers { get; set; } = new Dictionary<string, PrinterConfig>();

        /// <summary>
        /// Gets or sets the UDP port used for discovery beacons.
        /// </summary>
        public int UdpBeaconPort { get; set; } = 8001;

        /// <summary>
        /// Gets or sets the collection of properties for printers indexed by their serial number.
        /// </summary>
        public Dictionary<string, PrinterProperties> PrintersProperties { get; set; } = new Dictionary<string, PrinterProperties>();

        /// <summary>
        /// Gets or sets a comma-separated list of ports to be excluded from automatic detection.
        /// </summary>
        public string ExcludePortList { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timeout in seconds for device detection.
        /// </summary>
        public int DetectionTimeout { get; set; } = 30;

        /// <summary>
        /// Gets or sets the web access security settings for the service.
        /// </summary>
        public WebAccessOptions WebAccess { get; set; } = new WebAccessOptions();

        /// <summary>
        /// Updates the provided payment mapping dictionary based on the stored configurations for a specific printer.
        /// </summary>
        /// <param name="serialNumber">The serial number of the printer to remap.</param>
        /// <param name="map">The dictionary of payment types to be updated.</param>
        public void RemapPaymentTypes(string serialNumber, Dictionary<PaymentType, string> map) 
        {
            _rwLock.EnterReadLock();
            if (PrintersProperties.TryGetValue(serialNumber, out PrinterProperties? printerProperties))
            {
                foreach (PaymentType pmt in (PaymentType[])Enum.GetValues(typeof(PaymentType)))
                {
                    var serializedKey = JsonConvert.SerializeObject(pmt).Trim('"');
                    if (printerProperties.PaymentTypeMappings.TryGetValue(serializedKey, out var newValue))
                    {
                        if (!string.IsNullOrEmpty(newValue)) {
                            map[pmt] = newValue;
                        }
                    }
                }
            }
            _rwLock.ExitReadLock();
        }

        /// <summary>
        /// Updates the provided <see cref="DeviceInfo"/> object with hardware constants 
        /// (like max text lengths) stored in the printer properties.
        /// </summary>
        /// <param name="info">The device info object to reconfigure.</param>
        public void ReconfigurePrinterConstants(DeviceInfo info)
        {
            _rwLock.EnterReadLock();
            try
            {
                if (!PrintersProperties.TryGetValue(info.SerialNumber, out PrinterProperties? printerProperties))
                {
                    printerProperties = new PrinterProperties();
                    PrintersProperties.Add(info.SerialNumber, printerProperties);
                }

                if (printerProperties.PrinterConstants.TryGetValue(
                    "commentTextMaxLength",
                    out var commentTextMaxLength))
                {
                    if (int.TryParse(commentTextMaxLength, out int value))
                    {
                        if (value > 0)
                        {
                            info.CommentTextMaxLength = value;
                        }
                    }
                }

                if (printerProperties.PrinterConstants.TryGetValue(
                    "itemTextMaxLength",
                    out var itemTextMaxLength))
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
            finally
            {
                _rwLock.ExitReadLock();
            }            
        }

        /// <summary>
        /// Updates the provided <see cref="DeviceInfo"/> object with operational options 
        /// (like payment terminal support) stored in the printer properties.
        /// </summary>
        /// <param name="info">The device info object to reconfigure.</param>
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
                 
                if (printerProperties.PrinterOptions.TryGetValue(
                    "supportPaymentTerminal",
                    out var supportPaymentTerminal))
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

                if (printerProperties.PrinterOptions.TryGetValue(
                    "usePaymentTerminal",
                    out var usePaymentTerminal))
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