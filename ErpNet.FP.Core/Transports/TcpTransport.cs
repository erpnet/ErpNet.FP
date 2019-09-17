using System;
using System.Collections.Generic;
using System.Net.Sockets;
using ErpNet.FP.Core.Logging;

namespace ErpNet.FP.Core.Transports
{
    /// <summary>
    /// TCP/IP transport.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Core.Transport" />
    public class TcpTransport : Transport
    {
        public override string TransportName => "tcp";

        protected readonly int DefaultPort = 9100;

        private readonly IDictionary<string, TcpTransport.Channel?> openedChannels =
            new Dictionary<string, TcpTransport.Channel?>();

        public override IChannel OpenChannel(string address)
        {
            if (openedChannels.TryGetValue(address, out Channel? channel))
            {
                if (channel == null)
                {
                    throw new TimeoutException("disabled due to timeout");
                }
                return channel;
            }
            else
            {
                try
                {
                    var (hostName, port) = ParseAddress(address);
                    channel = new Channel(hostName, port);
                    openedChannels.Add(address, channel);
                    return channel;
                }
                catch (TimeoutException e)
                {
                    openedChannels.Add(address, null);
                    throw e;
                }
            }
        }

        public override void Drop(IChannel channel)
        {
            ((Channel)channel).Close();
        }

        protected (string, int) ParseAddress(string address)
        {
            var parts = address.Split(':');
            if (parts.Length == 1) return (address, DefaultPort);
            var hostName = parts[0];
            var port = parts.Length > 1 ? int.Parse(parts[1]) : DefaultPort;
            return (hostName, port);
        }

        public class Channel : IChannel
        {
            private readonly TcpClient tcpClient;
            private readonly NetworkStream netStream;

            private string HostName { get; }
            private int Port { get; }

            public string Descriptor => $"{HostName}:{Port}";

            public Channel(string hostName, int port)
            {
                HostName = hostName;
                Port = port;
                tcpClient = new TcpClient();
                netStream = ConnectAndGetStream();
            }

            protected NetworkStream ConnectAndGetStream()
            {
                var task = tcpClient.ConnectAsync(HostName, Port);
                if (task.Wait(Math.Max(200, tcpClient.ReceiveTimeout)))
                {
                    return tcpClient.GetStream();
                }
                var errorMessage = $"Timeout occured while connecting to {HostName}:{Port}";
                Log.Error(errorMessage);
                throw new TimeoutException(errorMessage);
            }

            public void Dispose()
            {
                Close();
            }

            public void Close()
            {
                tcpClient.Close();
                // Closing the tcpClient instance does not close the network stream.
                netStream.Close();
            }

            /// <summary>
            /// Reads data from the tcp connection.
            /// </summary>
            /// <returns>The data which was read.</returns>
            public byte[] Read()
            {
                var buffer = new byte[tcpClient.ReceiveBufferSize];
                var task = netStream.ReadAsync(buffer, 0, buffer.Length);
                if (task.Wait(netStream.ReadTimeout))
                {
                    var result = new byte[task.Result];
                    Array.Copy(buffer, result, task.Result);
                    return result;
                }
                var errorMessage = $"Timeout occured while reading from tcp connection {HostName}:{Port}";
                Log.Error(errorMessage);
                throw new TimeoutException(errorMessage);
            }

            /// <summary>
            /// Writes the specified data to the tcp connection.
            /// </summary>
            /// <param name="data">The data to write.</param>
            public void Write(byte[] data)
            {
                if (!tcpClient.Connected)
                {
                    tcpClient.Connect(HostName, Port);
                }
                var bytesToWrite = data.Length;
                while (bytesToWrite > 0)
                {
                    var writeSize = Math.Min(bytesToWrite, tcpClient.SendBufferSize);
                    var task = netStream.WriteAsync(
                        data,
                        data.Length - bytesToWrite,
                        writeSize
                    );
                    if (task.Wait(netStream.WriteTimeout))
                    {
                        bytesToWrite -= writeSize;
                    }
                    else
                    {
                        var errorMessage = $"Timeout occured while writing to tcp connection {HostName}:{Port}";
                        Log.Error(errorMessage);
                        throw new TimeoutException(errorMessage);
                    }
                }
            }
        }

    }
}