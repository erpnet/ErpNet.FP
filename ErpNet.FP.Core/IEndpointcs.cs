using System;
using System.Collections.Generic;
using System.Text;

namespace ErpNet.FP.Core
{
    public class FiscalPrinterOperator
    {
        public string Operator;
        public string Password;
    }

    /// <summary>
    /// Helps with discoverablity
    /// </summary>
    public interface IApiEndpoint
    {
        /// <summary>
        /// Returns true if specific
        /// </summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Attempts to auto-discover COM port the device uses
        /// </summary>
        /// <returns></returns>
        FiscalPrinterPort AutoDiscoverComPort();

        FiscalPrinterPort Port { get; set; }
        FiscalPrinterOperator Operator { get; set; }
    }
}
