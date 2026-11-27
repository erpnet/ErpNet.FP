namespace ErpNet.FP.Core.Transports
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Text;
    using Serilog;

    /// <summary>
    /// TCP/IP transport.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Core.Transport" />
    public class TcpTransport : Transport
    {
        public override string TransportName => "tcp";

        protected readonly int DefaultPort = 9100;

        private readonly IDictionary<(string Address, string? Password), TcpTransport.Channel?> openedChannels =
            new Dictionary<(string Address, string? Password), TcpTransport.Channel?>();
        private readonly IDictionary<(string Address, string? Password), TcpTransport.Channel?> openedTremolChannels =
            new Dictionary<(string Address, string? Password), TcpTransport.Channel?>();

        public override IChannel OpenChannel(string address)
        {
            return OpenChannel(address, null, openedChannels);
        }

        internal IChannel OpenTremolChannel(string address, string? connectionPassword)
        {
            return OpenChannel(address, connectionPassword, openedTremolChannels);
        }

        internal void InvalidateTremolChannel(string address, string? connectionPassword)
        {
            var channelKey = (address, connectionPassword);
            if (openedTremolChannels.TryGetValue(channelKey, out Channel? channel))
            {
                openedTremolChannels.Remove(channelKey);
                if (channel != null)
                {
                    try
                    {
                        channel.Close();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Cannot close TREMOL TCP channel {address}: {ex.GetType().Name}");
                    }
                }
            }
        }

        private IChannel OpenChannel(
            string address,
            string? connectionPassword,
            IDictionary<(string Address, string? Password), TcpTransport.Channel?> channelCache)
        {
            var channelKey = (address, connectionPassword);
            if (channelCache.TryGetValue(channelKey, out Channel? channel))
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
                    channel = new Channel(hostName, port, connectionPassword);
                    channelCache.Add(channelKey, channel);
                    return channel;
                }
                catch (TimeoutException)
                {
                    channelCache.Add(channelKey, null);
                    throw;
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
            private TcpClient tcpClient;
            private NetworkStream netStream;

            private string HostName { get; }
            private int Port { get; }
            private string? ConnectionPassword { get; }

            public string Descriptor => $"{HostName}:{Port}";

            public Channel(string hostName, int port)
                : this(hostName, port, null)
            {
            }

            internal Channel(string hostName, int port, string? connectionPassword)
            {
                HostName = hostName;
                Port = port;
                ConnectionPassword = connectionPassword;
                tcpClient = new TcpClient();
                netStream = ConnectAndGetStream();
            }

            protected NetworkStream ConnectAndGetStream()
            {
                var task = tcpClient.ConnectAsync(HostName, Port);
                if (task.Wait(Math.Max(200, tcpClient.ReceiveTimeout)))
                {
                    NetworkStream stream;
                    try
                    {
                        stream = tcpClient.GetStream();
                    }
                    catch
                    {
                        tcpClient.Dispose();
                        throw;
                    }

                    try
                    {
                        if (ConnectionPassword != null)
                        {
                            var passwordBytes = Encoding.UTF8.GetBytes(ConnectionPassword);
                            var authenticationBytes = new byte[passwordBytes.Length + 1];
                            Array.Copy(passwordBytes, authenticationBytes, passwordBytes.Length);
                            authenticationBytes[authenticationBytes.Length - 1] = 0x0A;
                            stream.Write(authenticationBytes, 0, authenticationBytes.Length);
                        }
                        return stream;
                    }
                    catch
                    {
                        stream.Dispose();
                        tcpClient.Dispose();
                        throw;
                    }
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

            private void Reconnect()
            {
                try
                {
                    Close();
                }
                finally
                {
                    // Closing TcpClient disposes instance
                    tcpClient = new TcpClient();
                    netStream = ConnectAndGetStream();
                }
            }

            /// <summary>
            /// Writes the specified data to the tcp connection.
            /// </summary>
            /// <param name="data">The data to write.</param>
            public void Write(byte[] data)
            {
                if (!tcpClient.Connected)
                {
                    try
                    {
                        Reconnect();
                    } 
                    catch (Exception inner)
                    {
                        var errorMessage = $"Cannot reconnect to {HostName}:{Port}";
                        Log.Error(errorMessage);
                        throw new TimeoutException(errorMessage, inner);
                    }
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