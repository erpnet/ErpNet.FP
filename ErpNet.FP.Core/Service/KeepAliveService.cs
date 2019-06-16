using ErpNet.FP.Core.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ErpNet.FP.Server.Services
{

    /// <summary>
    /// KeepAliveService implements IHostedService, but can be manually used
    /// with StartAsync/StopAsync methods for implementations that 
    /// does not provide support for IHostedService
    /// </summary>
    public class KeepAliveService : IDisposable
    {
        private readonly IServiceController Context;
        private Timer? Timer;

        public KeepAliveService(
            IServiceController context)
        {
            this.Context = context;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            System.Diagnostics.Trace.WriteLine("Keep Alive Background Service is starting.");

            // Get Status every 120 seconds, for keeping alive the connection
            Timer = new Timer(KeepAliveWithGetStatus, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(120));

            return Task.CompletedTask;
        }

        private void KeepAliveWithGetStatus(object state)
        {
            System.Diagnostics.Trace.WriteLine("Keep Alive Background Service running...");
            try
            {
                foreach (var printer in Context.Printers)
                {
                    if (Context.IsReady) printer.Value.CheckStatus();
                }
                System.Diagnostics.Trace.WriteLine("Keep Alive Background Service done.");
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine($"Error occured while keeping alive with get status: {e.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            System.Diagnostics.Trace.WriteLine("Keep Alive Background Service is stopping.");

            Timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Timer?.Dispose();
        }
    }
}
