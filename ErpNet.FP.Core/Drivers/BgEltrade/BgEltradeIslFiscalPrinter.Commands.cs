using ErpNet.FP.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgEltrade
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Eltrade Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgEltradeIslFiscalPrinter : BgIslFiscalPrinter
    {
        protected const byte
            EltradeCommandOpenFiscalReceipt = 0x90;

        protected override DeviceStatus ParseStatus(byte[]? status)
        {
            // TODO: Device status parser
            return new DeviceStatus();
        }

        public override (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber)
        {
            var header = string.Join(",",
                new string[] {
                    Options.ValueOrDefault("Operator.Name", "Operator"),
                    uniqueSaleNumber
                });
            return Request(CommandOpenFiscalReceipt, header);
        }
    }
}
