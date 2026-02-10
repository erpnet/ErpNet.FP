namespace ErpNet.FP.Core.Service
{
    using ErpNet.FP.Core.Configuration;

    /// <summary>
    /// Represents the global runtime variables and state configuration for the ErpNet.FP service.
    /// </summary>
    public class ServerVariables
    {
        /// <summary>
        /// Gets or sets the current version string of the service.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique identifier for this specific server instance.
        /// </summary>
        public string ServerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the service should automatically 
        /// scan for and detect connected fiscal devices.
        /// </summary>
        public bool AutoDetect { get; set; } = true;

        /// <summary>
        /// Gets or sets the UDP port used for broadcasting discovery beacons 
        /// to let clients find the service on the network.
        /// </summary>
        public int UdpBeaconPort { get; set; } = 8001;

        /// <summary>
        /// Gets or sets a comma-separated list of communication ports that should 
        /// be ignored during the automatic detection process.
        /// </summary>
        public string ExcludePortList { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the duration in seconds the service will wait for 
        /// a response during device discovery.
        /// </summary>
        public int DetectionTimeout { get; set; } = 30;

        /// <summary>
        /// Gets or sets the security and access settings for web-based requests, 
        /// handling cross-origin and private network policies.
        /// </summary>
        public WebAccessOptions WebAccess { get; set; } = new();
    }
}