using ErpNet.FP.Core;
using System;

namespace ErpNet.FP.CoreExample
{
    /// <summary>
    /// (Example of) Transport, which connects with ErpNet.FP.CloudPrint instances.
    /// </summary>
    public class CloudPrintTransport : Transport
    {
        public CloudPrintTransport(string userName, string password)
        {

        }

        public override string TransportName => "cloud";

        public override IChannel OpenChannel(string address)
        {
            throw new NotImplementedException();
        }
    }
}
