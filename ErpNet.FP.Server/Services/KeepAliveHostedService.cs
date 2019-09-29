namespace ErpNet.FP.Server.Services
{
    using ErpNet.FP.Core.Service;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// KeepAliveHostedService is wrapper around KeepAliveService
    /// which declares IHostedService implementation, and dependency
    /// injection of IServiceController instance
    /// </summary>
    public class KeepAliveHostedService : KeepAliveService, IHostedService
    {
        public KeepAliveHostedService(
            IServiceController context)
            :
            base(context)
        { }
    }
}
