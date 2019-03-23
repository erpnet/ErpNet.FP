using System;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Example
{
    /// <summary>
    /// Serial COM port transport.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Print.Core.Transport" />
    public class ComTransport : Transport
    {
        public override string TransportName => "com";

        public override IChannel OpenChannel(string address)
        {
            throw new NotImplementedException();
        }
    }
}
