using System.Collections.Generic;
using System.Linq;

namespace ErpNet.FP.Core
{
    /// <summary>
    /// Represents the physical transport protocol like serial COM, Bluetooth, http, etc.
    /// </summary>
    public abstract class Transport
    {
        /// <summary>
        /// Returns the transport protocol name. For example - http, com (for com port serial), bt (bluetooth), etc.
        /// </summary>
        public abstract string TransportName { get; }

        /// <summary>
        /// Returns all (usually local) addresses, which can have connected fiscal printers. 
        /// The returned pairs are in the form <see cref="KeyValuePair{Address, Description}"/>.
        /// </summary>
        /// <value>
        /// All available addresses and descriptions.
        /// </value>
        public virtual IEnumerable<(string address, string description)> GetAvailableAddresses()
        {
            return Enumerable.Empty<(string, string)>();
        }

        /// <summary>
        /// Opens a channel to the specified address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns>The newly created channel.</returns>
        public abstract IChannel OpenChannel(string address);
    }
}
