using System;

namespace ErpNet.FP.Core.Transports
{
    public class HttpTransport : Transport
    {
        public override string TransportName => "http";

        public override IChannel OpenChannel(string address)
        {
            throw new NotImplementedException();
        }

        public byte[] Read()
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
