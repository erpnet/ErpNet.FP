using ErpNet.FP.Print.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Print.Drivers.BgEltrade
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Eltrade Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public class BgEltradeIslFiscalPrinter : BgIslFiscalPrinter
    {
        protected const byte
            EltradeCommandAbortFiscalReceipt = 0x3c,
            EltradeCommandOpenFiscalReceipt = 0x90;

        public BgEltradeIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options)
        {
        }
        protected override DeviceStatus ParseStatus(byte[] status)
        {
            // TODO: Device status parser
            return new DeviceStatus();
        }

        public override (string, DeviceStatus) AbortReceipt()
        {
            return Request(EltradeCommandAbortFiscalReceipt);
        }

        public override (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber, string operatorID, string operatorPassword)
        {
            var header = string.Join(",",
                new string[] {
                    operatorID,
                    uniqueSaleNumber
                });
            return Request(EltradeCommandOpenFiscalReceipt, header);
        }

    }
}
