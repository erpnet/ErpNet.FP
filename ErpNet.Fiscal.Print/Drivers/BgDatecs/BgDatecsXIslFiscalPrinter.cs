using ErpNet.Fiscal.Print.Core;
using System.Collections.Generic;

namespace ErpNet.Fiscal.Print.Drivers.BgDatecs
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Datecs Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.Fiscal.Drivers.BgIslFiscalPrinter" />
    public class BgDatecsXIslFiscalPrinter : BgIslFiscalPrinter
    {
        public BgDatecsXIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options)
        {
        }

        protected override DeviceStatus ParseStatus(byte[] status)
        {
            // TODO: Device status parser
            return new DeviceStatus();
        }

    }
}
