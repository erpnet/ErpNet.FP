using ErpNet.FP.Core.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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
        private const int UdpBeaconPort = 8001;

        private Timer? Timer;
        private List<Uri>? UriList;

        public static IServerAddressesFeature? ServerAddresses;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Beacon service description every 3 seconds
            Timer = new Timer(BeaconServiceDescription, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(3));

            Log.Information("Simple Discovery Service is started.");

            return Task.CompletedTask;
        }

        private void BeaconServiceDescription(object? state)
        {
            if (ServerAddresses != null)
            {
                if (UriList == null)
                {
                    UriList = new List<Uri>();
                    foreach (var address in ServerAddresses.Addresses)
                    {
                        UriList.Add(new Uri(address));
                    }
                } else {
                    foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.OperationalStatus == OperationalStatus.Up && ni.SupportsMulticast && ni.GetIPProperties().GetIPv4Properties() != null)
                        {
                            int id = ni.GetIPProperties().GetIPv4Properties().Index;
                            if (NetworkInterface.LoopbackInterfaceIndex != id)
                            {
                                foreach (UnicastIPAddressInformation uip in ni.GetIPProperties().UnicastAddresses)
                                {
                                    if (uip.Address.AddressFamily == AddressFamily.InterNetwork)
                                    {
                                        IPEndPoint local = new IPEndPoint(uip.Address, 0);
                                        IPEndPoint bcast = new IPEndPoint(GetBroadcastAddress(uip.Address, uip.IPv4Mask), UdpBeaconPort);

                                        var sb = new StringBuilder();
                                        foreach (var uri in UriList)
                                        {
                                            sb.Append($"{uri.Scheme}://{local.Address}:{uri.Port};");
                                        }
                                        var ServiceDescription = Encoding.UTF8.GetBytes($"ErpNet.FP: {sb}");

                                        try
                                        {
                                            using BroadcastUdpClient udpc = new BroadcastUdpClient(local);
                                            udpc.Send(ServiceDescription, ServiceDescription.Length, bcast);
                                        }
                                        catch(Exception ex)
                                        {
                                            Log.Error($"Problem while sending the service discovery beacon: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Timer?.Change(Timeout.Infinite, 0);

            Log.Information("Simple Discovery Service is stopped.");

            return Task.CompletedTask;
        }

        private static IPAddress GetBroadcastAddress(UnicastIPAddressInformation unicastAddress)
        {
            return GetBroadcastAddress(unicastAddress.Address, unicastAddress.IPv4Mask);
        }

        private static IPAddress GetBroadcastAddress(IPAddress address, IPAddress mask)
        {
            uint ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
            uint ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
            uint broadCastIpAddress = ipAddress | ~ipMaskV4;

            return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
        }

        public void Dispose()
        {
            Timer?.Dispose();
        }
    }

    public class BroadcastUdpClient : UdpClient
    {
        public BroadcastUdpClient(IPEndPoint ipLocalEndPoint) : base(ipLocalEndPoint)
        {
            EnableBroadcast = true;
            //Calls the protected Client property belonging to the UdpClient base class.
            Socket s = this.Client;
            //Uses the Socket returned by Client to set an option that is not available using UdpClient.
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, 1);
        }
    }

}
