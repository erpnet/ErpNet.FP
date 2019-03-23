using System;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Example
{
    /// <summary>
    /// Bluetooth transport.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Print.Core.Transport" />
    public class BtTransport : Transport
    {
        public override string TransportName => "bt";

        public override IChannel OpenChannel(string address)
        {
            throw new NotImplementedException();
        }
    }
}
