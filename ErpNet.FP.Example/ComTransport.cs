using System.Threading;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using ErpNet.FP.Print.Core;
using System.Threading.Tasks;

namespace ErpNet.FP.Example {
	/// <summary>
	/// Serial COM port transport.
	/// </summary>
	/// <seealso cref="ErpNet.FP.Print.Core.Transport" />
	public class ComTransport : Transport {
		public override string TransportName => "com";

		private IDictionary<string, ComTransport.Channel> openedChannels =
            new Dictionary<string, ComTransport.Channel>();

		public override IChannel OpenChannel (string address) {
			if (openedChannels.ContainsKey(address)) {
				return openedChannels[address];
			}
			var channel = new Channel(address);
			openedChannels.Add(address, channel);
			return channel;
		}

		/// <summary>
		/// Returns all serial com port addresses, which can have connected fiscal printers. 
		/// The returned pairs are in the form <see cref="KeyValuePair{Address, Description}"/>.
		/// </summary>
		/// <value>
		/// All available addresses and descriptions.
		/// </value>
		public override IEnumerable < (string address, string description) > GetAvailableAddresses () {
			// For description of com ports we do not have anything else than the port name / port path
			return from address in SerialPort.GetPortNames() select (address, address);
		}

		public class Channel : IChannel {
			private SerialPort _serialPort;

			public string Descriptor => _serialPort.PortName;

			public Channel (string portName, int baudRate = 115200, int timeout = 500) {
				_serialPort = new SerialPort();

				// Allow the user to set the appropriate properties.
				_serialPort.PortName = portName;
				_serialPort.BaudRate = baudRate;

				// Set the read/write timeouts
				_serialPort.ReadTimeout = timeout;
				_serialPort.WriteTimeout = timeout;

				_serialPort.Open ();
			}

			public void Dispose(bool disposing) {
				if (disposing) {
					_serialPort.Close ();
				}
			}

			public void Dispose()
			{
				// Dispose of unmanaged resources.
				Dispose(true);
				// Suppress finalization.
				GC.SuppressFinalize(this);
			}

			/// <summary>
			/// Reads data from the com port.
			/// </summary>
			/// <returns>The data which was read.</returns>
			public byte[] Read () {
				var buffer = new byte[_serialPort.ReadBufferSize];
				var task = _serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length);
				task.Wait(_serialPort.ReadTimeout);
				var result = new byte[task.Result];
				Array.Copy(buffer, result, task.Result);
				return result;
			}

			/// <summary>
			/// Writes the specified data to the com port.
			/// </summary>
			/// <param name="data">The data to write.</param>
			public void Write (Byte[] data) {
				_serialPort.DiscardInBuffer();
				var bytesToWrite = data.Length;
				while (bytesToWrite>0) {
					var writeSize = Math.Min(bytesToWrite, _serialPort.WriteBufferSize);
					var task = _serialPort.BaseStream.WriteAsync(
						data, 
						data.Length-bytesToWrite, 
						writeSize
					);
					task.Wait(_serialPort.WriteTimeout);
					bytesToWrite -= writeSize;
				}
			}
		}

	}
}