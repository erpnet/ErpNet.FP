namespace ErpNet.FP.Core.Service
{
    using ErpNet.FP.Core.Configuration;

    public class ServerVariables
    {
        public string Version = string.Empty;
        public string ServerId = string.Empty;
        public bool AutoDetect = true;
        public int UdpBeaconPort = 8001;
        public WebAccessOptions WebAccess = new WebAccessOptions();
    }
}