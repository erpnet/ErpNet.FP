using ErpNet.Fiscal.Print.Core;
using System.Collections.Generic;

namespace ErpNet.Fiscal.Print.Drivers.BgDatecs
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Datecs Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.Fiscal.Drivers.BgIslFiscalPrinter" />
    public class BgDatecsCIslFiscalPrinter : BgIslFiscalPrinter
    {
        public BgDatecsCIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options)
        {
        }

        public override (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber, string operatorID, string operatorPassword)
        {
            var header = string.Join(",",
                new string[] {
                    operatorID,
                    operatorPassword.WithMaxLength(Info.OperatorPasswordMaxLength),
                    uniqueSaleNumber,
                    "1"
                });
            return Request(CommandOpenFiscalReceipt, header);
        }

        protected override DeviceStatus ParseStatus(byte[] status)
        {
            // TODO: Device status parser
            return new DeviceStatus();
        }

    }
}
