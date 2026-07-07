namespace ErpNet.FP.Core.Drivers.BgSis
{
    using ErpNet.FP.Core.Transports;

    /// <summary>
    /// <see cref="HttpTransport"/> specialized for the SIS Fiscal Module's JSON-RPC endpoint.
    /// </summary>
    public class BgSisJsonHttpTransport : HttpTransport
    {
        public BgSisJsonHttpTransport()
            : base(defaultPath: "/jsonrpc", contentType: "application/json; charset=utf-8")
        {
        }
    }
}
