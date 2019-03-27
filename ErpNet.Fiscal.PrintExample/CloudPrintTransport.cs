using System;
using ErpNet.Fiscal.Print.Core;

namespace ErpNet.Fiscal.PrintExample
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
