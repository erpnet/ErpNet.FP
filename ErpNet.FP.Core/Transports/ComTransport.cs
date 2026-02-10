namespace ErpNet.FP.Core.Transports
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Ports;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Serilog;

    public class ComTransport : Transport
    {
        public override string TransportName => "com";

        protected const int DefaultBaudRate = 115200;
        protected const int DefaultTimeout = 800;
        protected const int DefaultTimeoutToClose = 3000;   // dispose SerialPort object when specified time after communication is elapsed

        private readonly IDictionary<string, ComTransport.Channel?> openedChannels =
            new Dictionary<string, ComTransport.Channel?>();

        public override IChannel OpenChannel(string address)
        {
            var (comPort, baudRate) = ParseAddress(address);
            if (openedChannels.TryGetValue(comPort, out Channel? channel))
            {
                if (channel != null)
                    return channel;
                else 
                    openedChannels.Remove(comPort);
            }
            channel = new Channel(comPort, baudRate);
            // <address> is more specific (<Com>:<Speed>) than <comPort> and multiple addresses on one port can lead to locking or not finding device (only one can be opened)
            openedChannels.Add(comPort, channel); 
            return channel;
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
            internal /*readonly*/ SerialPort? serialPort;    // can be disposed and created multiple times
            private string portName;       
            private int baudRate;
            protected Timer idleTimer;
            protected const int MinimalBaudRate = 9600;

            public string Descriptor
            {
                get
                {
                    if (serialPort != null)
                    {
                        return serialPort.PortName +
                            (serialPort.BaudRate == DefaultBaudRate ? "" : $":{serialPort.BaudRate}");
                    }
                    else
                    {
                        return portName + (baudRate == DefaultBaudRate ? "" : $":{baudRate}");
                    }
                }
            }

            public SerialPort GetNewSerialPort(int timeout = DefaultTimeout)
            {
                idleTimer.Change(DefaultTimeoutToClose, 0);
                return new SerialPort
                {
                    // Allow the user to set the appropriate properties.
                    PortName = portName,
                    BaudRate = baudRate,
                    
                    // Set the read/write timeouts
                    ReadTimeout = timeout,
                    WriteTimeout = timeout
                };
            }

            public Channel(string portName, int baudRate = DefaultBaudRate, int timeout = DefaultTimeout)
            {
                this.portName = portName;
                this.baudRate = baudRate;

                serialPort = null;  // GetNewSerialPort();
                idleTimer = new Timer(IdleTimerElapsed); 
                idleTimer.Change(DefaultTimeoutToClose, 0);
            }

            private void IdleTimerElapsed(object? state)
            {
                if (Monitor.TryEnter(this))
                {
                    try
                    { 
                        if (serialPort != null)
                            Log.Information($"Idle timer elapsed for the com port {this.portName}");
                        Close();
                    }
                    finally
                    {
                        Monitor.Exit(this);
                    }
                }
            }

            public void Open()
            {
                if (serialPort == null)
                    serialPort = GetNewSerialPort();
                idleTimer.Change(DefaultTimeoutToClose, 0);

                try
                {
                    if (!serialPort.IsOpen)
                    {
                        Log.Information($"Opening the com port {serialPort.PortName}");
                        serialPort.Open();
                        Log.Information($"Com port {serialPort.PortName} opened successfully!");
                    }
                    //else
                    //    Log.Information($"Com port {serialPort.PortName} already opened!");
                }
                catch (FileNotFoundException)
                {
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.Information($"Access denied for {serialPort.PortName}: {ex.Message}. Maybe the port is already open from another application or driver.");
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Information($"Error while opening {portName}: {ex.Message}. Trying baudrate {MinimalBaudRate}...");
                    if (serialPort != null)
                        serialPort.Dispose();           // Dispose and get new SerialPort object before trying new speed
                    serialPort = GetNewSerialPort();
                    // Trying to open the port at minimal baudrate
                    serialPort.BaudRate = MinimalBaudRate;  
                    serialPort.Open();  // will throw another exception if not working with MinimalBaudRate
                }
            }

            public void Close()
            {
                if (serialPort != null)
                {
                    Log.Information($"Closing the com port {this.portName}");
                    try
                    {
                        serialPort.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error while closing the com port {this.portName}: {ex.Message}");
                    }
                    finally
                    {
                        serialPort = null;
                    }
                }
            }

            //    not called ever
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
                if (serialPort == null)
                {
                    throw new FileNotFoundException("Can't read from unexistent serial port!");
                }
                var buffer = new byte[serialPort.ReadBufferSize];
                var task = serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length);
                if (task.Wait(serialPort.ReadTimeout))
                {
                    var result = new byte[task.Result];
                    Array.Copy(buffer, result, task.Result);
                    idleTimer.Change(DefaultTimeoutToClose, 0);
                    Log.Information($"Read {result.Length} bytes from the com port {this.portName}");
                    return result;
                }
                idleTimer.Change(DefaultTimeoutToClose, 0);
                var errorMessage = $"Timeout occured while reading from com port '{serialPort.PortName}'";
                Log.Error(errorMessage);
                try
                {
                    serialPort.Close();
                }
                finally
                {
                    throw new TimeoutException(errorMessage);
                }
            }

            /// <summary>
            /// Writes the specified data to the com port.
            /// </summary>
            /// <param name="data">The data to write.</param>
            public void Write(byte[] data)
            {
                //Log.Information($"Before Writing {data.Length} bytes to the com port {this.portName}");
                Monitor.Enter(this);
                try
                {
                    idleTimer.Change(-1, 0);
                    Open();

                    if (serialPort == null)
                    {
                        throw new FileNotFoundException("Can't write to an unavailable serial port!");
                    }

                    serialPort.DiscardInBuffer();
                    var bytesToWrite = data.Length;
                    while (bytesToWrite > 0)
                    {
                        var writeSize = Math.Min(bytesToWrite, serialPort.WriteBufferSize);
                        //Log.Information($"Writing {writeSize} bytes to the com port {this.portName}");
                        var task = serialPort.BaseStream.WriteAsync(
                            data,
                            data.Length - bytesToWrite,
                            writeSize
                        );
                        if (task.Wait(serialPort.WriteTimeout))
                        {
                            bytesToWrite -= writeSize;
                            idleTimer.Change(DefaultTimeoutToClose, 0);
                            Log.Information($"Write completed to the com port {portName}");
                        }
                        else
                        {
                            var errorMessage = $"Timeout occured while writing to com port '{portName}'";
                            Log.Error(errorMessage);
                            idleTimer.Change(DefaultTimeoutToClose, 0);
                            throw new TimeoutException(errorMessage);
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }
        }

    }
}