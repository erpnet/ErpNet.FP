using ErpNet.Fiscal.Print.Core;
using System.Collections.Generic;

namespace ErpNet.Fiscal.Print.Drivers.BgDatecs
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Datecs Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.Fiscal.Drivers.BgIslFiscalPrinter" />
    public class BgDatecsPIslFiscalPrinter : BgIslFiscalPrinter
    {
        public BgDatecsPIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options) { }

        public override IDictionary<string, string> GetDefaultOptions()
        {
            return new Dictionary<string, string>
            {
                ["Operator.ID"] = "1",
                ["Operator.Password"] = "0000",

                ["Administrator.ID"] = "20",
                ["Administrator.Password"] = "9999"
            };
        }

        public override (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber, string operatorID, string operatorPassword)
        {
            var header = string.Join(",",
                new string[] {
                    operatorID,
                    operatorPassword.WithMaxLength(Info.OperatorPasswordMaxLength),
                    "1",
                    uniqueSaleNumber
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
