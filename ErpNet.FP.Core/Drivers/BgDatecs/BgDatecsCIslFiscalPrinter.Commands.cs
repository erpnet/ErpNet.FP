using ErpNet.FP.Core;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Datecs Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgDatecsCIslFiscalPrinter : BgIslFiscalPrinter
    {
        public override (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber)
        {
            var header = string.Join(",",
                new string[] {
                    Options.ValueOrDefault("Operator.ID", "1"),
                    Options.ValueOrDefault("Operator.Password", "1").WithMaxLength(Info.OperatorPasswordMaxLength),
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
