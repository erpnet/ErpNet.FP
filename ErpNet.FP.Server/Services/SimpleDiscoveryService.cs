using ErpNet.FP.Core.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ErpNet.FP.Server.Services
{
    /// <summary>
    /// SimpleDiscoveryService is created for zero-configuration 
    /// setup for the clients of the ErpNet.FP service instances in the LAN
    /// It broadcasts every  seconds, the identifier "ErpNet.FP" to UDP port 8001.
    /// </summary>
    public class SimpleDiscoveryService : IDisposable, IHostedService
    {
        private const int UdpPort = 8001;
        private readonly UdpClient UdpClient = new UdpClient();
        private readonly IPEndPoint BroadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, UdpPort);
        private byte[] ServiceDescription = Encoding.UTF8.GetBytes("ErpNet.FP");
        private Timer? Timer;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            UdpClient.EnableBroadcast = true;

            // Beacon service description every 3 seconds
            Timer = new Timer(BeaconServiceDescription, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(3));

            Log.Information("Simple Discovery Service is started.");

            return Task.CompletedTask;
        }

        private void BeaconServiceDescription(object? state)
        {
            UdpClient.Send(ServiceDescription, ServiceDescription.Length, BroadcastEndPoint);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Timer?.Change(Timeout.Infinite, 0);

            Log.Information("Simple Discovery Service is stopped.");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Timer?.Dispose();
        }
    }
}
