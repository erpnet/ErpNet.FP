namespace ErpNet.FP.Core.Service
{
    public class ServerVariables
    {
        public string Version = string.Empty;
        public string ServerId = string.Empty;
        public bool AutoDetect = true;
        public int UdpBeaconPort = 8001;
        public string ExcludePortList = string.Empty;
        public int DetectionTimeout = 30;
    }
}