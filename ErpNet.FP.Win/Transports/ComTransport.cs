using ErpNet.FP.Core;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

namespace ErpNet.FP.Win.Transports
{
    /// <summary>
    /// Serial COM port transport.
    /// </summary>
    public class ComTransport : Transport
    {
        public override string TransportName => "com";

        private readonly IDictionary<string, ComTransport.Channel?> openedChannels =
            new Dictionary<string, ComTransport.Channel?>();

        public override IChannel OpenChannel(string address)
        {
            try
            {
                var channel = openedChannels[address];
                if (channel == null)
                {
                    throw new TimeoutException("disabled due to timeout");
                }
                return channel;
            }
            catch (KeyNotFoundException)
            {
                // There is no opened channel with this address
                // So we will create and open one
                try
                {
                    var channel = new Channel(address);
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
            private readonly SerialPort serialPort;

            public string Descriptor => serialPort.PortName;

            public Channel(string portName, int baudRate = 115200, int timeout = 600)
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

                serialPort.Open();
            }

            public void Dispose()
            {
                serialPort.Close();
            }

            /// <summary>
            /// Reads data from the com port.
            /// </summary>
            /// <returns>The data which was read.</returns>
            public byte[] Read()
            {
                var buffer = new byte[serialPort.ReadBufferSize];
                var task = serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length);
                if (task.Wait(serialPort.ReadTimeout))
                {
                    var result = new byte[task.Result];
                    Array.Copy(buffer, result, task.Result);
                    return result;
                }
                throw new TimeoutException($"timeout occured while reading from com port '{serialPort.PortName}'");
            }

            /// <summary>
            /// Writes the specified data to the com port.
            /// </summary>
            /// <param name="data">The data to write.</param>
            public void Write(byte[] data)
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
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
                        throw new TimeoutException($"timeout occured while writing to com port '{serialPort.PortName}'");
                    }
                }
            }
        }

    }
}