using System;
using System.Collections.Generic;
using System.Text;

namespace ErpNet.FP.Core
{
    /// <summary>
    /// Represents a concrete transport channel, created from a <see cref="Transport"/>.
    /// </summary>
    public interface IChannel
    {
        /// <summary>
        /// Reads data from the transport channel.
        /// </summary>
        /// <returns>The data which was read.</returns>
        Byte[] Read();

        /// <summary>
        /// Writes the specified data to the transport channel.
        /// </summary>
        /// <param name="data">The data to write.</param>
        void Write(Byte[] data);
        string Descriptor {get;}
    }
}
