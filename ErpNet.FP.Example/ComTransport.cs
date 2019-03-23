using System;
using System.IO.Ports;
using System.Linq;
using System.Collections.Generic;
using ErpNet.FP.Print.Core;

namespace ErpNet.FP.Example
{
	/// <summary>
	/// Serial COM port transport.
	/// </summary>
	/// <seealso cref="ErpNet.FP.Print.Core.Transport" />
	public class ComTransport : Transport
	{
		public override string TransportName => "com";

		public override IChannel OpenChannel(string address) => new Channel(address, 115200, 600);

		/// <summary>
		/// Returns all serial com port addresses, which can have connected fiscal printers. 
		/// The returned pairs are in the form <see cref="KeyValuePair{Address, Description}"/>.
		/// </summary>
		/// <value>
		/// All available addresses and descriptions.
		/// </value>
		public virtual IEnumerable<(string address, string description)> GetAvailableAddresses()
		{
			return from address in SerialPort.GetPortNames() select (address, address);
		}

		public class Channel : IChannel
		{
			private SerialPort _serialPort;

			public Channel(string portName, int baudRate, int timeout)
			{
				_serialPort = new SerialPort();

				// Allow the user to set the appropriate properties.
				_serialPort.PortName = portName;
				_serialPort.BaudRate = baudRate;

				// Set the read/write timeouts
				_serialPort.ReadTimeout = timeout;
				_serialPort.WriteTimeout = timeout;

				_serialPort.Open();
			}

			~Channel()
			{
				_serialPort.Close();
			}

			/// <summary>
			/// Reads data from the com port.
			/// </summary>
			/// <returns>The data which was read.</returns>
			public Byte[] Read()
			{
				var dataSize = _serialPort.BytesToRead;
				byte[] data = new byte[dataSize];
				_serialPort.Read(data, 0, dataSize);
				return data;
			}

			/// <summary>
			/// Writes the specified data to the com port.
			/// </summary>
			/// <param name="data">The data to write.</param>
			public void Write(Byte[] data)
			{
				_serialPort.Write(data, 0, data.Length);
			}
		}

	}
}
