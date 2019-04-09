using ErpNet.FP.Core;
using System;

namespace ErpNet.FP.CoreExample
{
    /// <summary>
    /// Bluetooth transport.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Core.Transport" />
    public class BtTransport : Transport
    {
        public override string TransportName => "bt";

        public override IChannel OpenChannel(string address)
        {
            throw new NotImplementedException();
        }
    }
}
