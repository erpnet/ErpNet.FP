using System;
using ErpNet.Fiscal.Print.Core;

namespace ErpNet.Fiscal.PrintExample
{
    /// <summary>
    /// Bluetooth transport.
    /// </summary>
    /// <seealso cref="ErpNet.Fiscal.Print.Core.Transport" />
    public class BtTransport : Transport
    {
        public override string TransportName => "bt";

        public override IChannel OpenChannel(string address)
        {
            throw new NotImplementedException();
        }
    }
}
