using ErpNet.FP.Core.Service;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ErpNet.FP.Server.Services
{
    public class KeepAliveHostedService : IHostedService, IDisposable
    {
        private readonly ILogger Logger;
        private readonly IServiceController Context;
        private Timer? Timer;

        public KeepAliveHostedService(
            ILogger<KeepAliveHostedService> logger,
            IServiceController context)
        {
            this.Logger = logger;
            this.Context = context;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Keep Alive Background Service is starting.");

            // Get Status every 120 seconds, for keeping alive the connection
            Timer = new Timer(KeepAliveWithGetStatus, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(120));

            return Task.CompletedTask;
        }

        private void KeepAliveWithGetStatus(object state)
        {
            Logger.LogInformation("Keep Alive Background Service running...");
            try
            {
                foreach (var printer in Context.Printers)
                {
                    if (Context.IsReady) printer.Value.CheckStatus();
                }
                Logger.LogInformation("Keep Alive Background Service done.");
            }
            catch (Exception e)
            {
                Logger.LogInformation($"Error occured while keeping alive with get status: {e.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Keep Alive Background Service is stopping.");

            Timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Timer?.Dispose();
        }
    }
}
