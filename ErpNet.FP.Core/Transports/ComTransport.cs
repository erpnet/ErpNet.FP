namespace ErpNet.FP.Core.Transports
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Ports;
    using System.Linq;
    using System.Threading;
    using Serilog;

    public class ComTransport : Transport
    {
        public override string TransportName => "com";

        protected const int DefaultBaudRate = 115200;
        protected const int DefaultTimeout = 1000;

        private readonly IDictionary<string, ComTransport.Channel?> openedChannels =
            new Dictionary<string, ComTransport.Channel?>();

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
                    var (comPort, baudRate) = ParseAddress(address);
                    channel = new Channel(comPort, baudRate);
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

        protected (string, int) ParseAddress(string address)
        {
            var parts = address.Split(':');
            if (parts.Length == 1) return (address, DefaultBaudRate);
            var hostName = parts[0];
            var baudRate = parts.Length > 1 ? int.Parse(parts[1]) : DefaultBaudRate;
            return (hostName, baudRate);
        }

        public override void Drop(IChannel channel)
        {
            ((Channel)channel).Close();
        }

        /// <summary>
        /// Returns all serial com port addresses, which can have connected fiscal printers. 
        /// The returned pairs are in the form <see cref="KeyValuePair{Address, Description}"/>.
        /// </summary>
        /// <value>
        /// All available addresses and descriptions.
        /// </value>
        public override IEnumerable<(string address, string description)> GetAvailableAddresses()
        {
            // For description of com ports we do not have anything else than the port name / path
            // So we will return port name as description too.
            return from address in SerialPort.GetPortNames() select (address, address);
        }

        public class Channel : IChannel
        {
            internal readonly SerialPort serialPort;
            protected Timer idleTimer;
            protected const int MinimalBaudRate = 9600;

            public string Descriptor
            {
                get
                {
                    return serialPort.PortName +
                        (serialPort.BaudRate == DefaultBaudRate ? "" : $":{serialPort.BaudRate}");
                }
            }

            public Channel(string portName, int baudRate = DefaultBaudRate, int timeout = DefaultTimeout)
            {
                serialPort = new SerialPort
                {
                    // Allow the user to set the appropriate properties.
                    PortName = portName,
                    BaudRate = baudRate,

                    // Set the read/write timeouts
                    ReadTimeout = timeout,
                    WriteTimeout = timeout
                };

                idleTimer = new Timer(IdleTimerElapsed);
            }

            private void IdleTimerElapsed(object state)
            {
                Log.Information($"Idle timer elapsed for the com port {serialPort.PortName}");
                Close();
            }

            public void Open()
            {
                Log.Information($"Opening the com port {serialPort.PortName}");
                try
                {
                    serialPort.Open();
                }
                catch (FileNotFoundException ex)
                {
                    throw ex;
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.Information($"Access denied for {serialPort.PortName}: {ex.Message}. Maybe the port is already open from another application or driver.");
                    throw ex;
                }
                catch (Exception ex)
                {
                    Log.Information($"Error while opening {serialPort.PortName}: {ex.Message}. Trying baudrate {MinimalBaudRate}...");
                    // Trying to open the port at minimal baudrate
                    serialPort.BaudRate = MinimalBaudRate;
                    serialPort.Open();
                }
            }

            public void Close()
            {
                if (serialPort.IsOpen)
                {
                    Log.Information($"Closing the com port {serialPort.PortName}");
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();
                    serialPort.Close();
                    serialPort.Dispose();
                }
            }

            public void Dispose()
            {
                idleTimer.Dispose();
                Close();
            }

            /// <summary>
            /// Reads data from the com port.
            /// </summary>
            /// <returns>The data which was read.</returns>
            public byte[] Read()
            {
                idleTimer.Change(-1, 0);
                var buffer = new byte[serialPort.ReadBufferSize];
                var task = serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length);
                if (task.Wait(serialPort.ReadTimeout))
                {
                    var result = new byte[task.Result];
                    Array.Copy(buffer, result, task.Result);
                    idleTimer.Change(1000, 0);
                    return result;
                }
                var errorMessage = $"Timeout occured while reading from com port '{serialPort.PortName}'";
                Log.Error(errorMessage);
                throw new TimeoutException(errorMessage);
            }

            /// <summary>
            /// Writes the specified data to the com port.
            /// </summary>
            /// <param name="data">The data to write.</param>
            public void Write(byte[] data)
            {
                idleTimer.Change(-1, 0);
                if (!serialPort.IsOpen)
                {
                    Open();
                }
                serialPort.DiscardInBuffer();
                var bytesToWrite = data.Length;
                while (bytesToWrite > 0)
                {
                    var writeSize = Math.Min(bytesToWrite, serialPort.WriteBufferSize);
                    var task = serialPort.BaseStream.WriteAsync(
                        data,
                        data.Length - bytesToWrite,
                        writeSize
                    );
                    if (task.Wait(serialPort.WriteTimeout))
                    {
                        bytesToWrite -= writeSize;
                    }
                    else
                    {
                        var errorMessage = $"Timeout occured while writing to com port '{serialPort.PortName}'";
                        Log.Error(errorMessage);
                        throw new TimeoutException(errorMessage);
                    }
                }
            }
        }

    }
}