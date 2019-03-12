using System;
using ErpNet.FP.Core;

namespace ErpNet.FP
{
    /// <summary>
    /// Factory methods for constructing and accessing fiscal printer drivers
    /// </summary>
    public static class FiscalPrinterFactory
    {
        /// <summary>
        /// Construct a new fiscal printer by type
        /// </summary>
        /// <param name="type">Driver type to use</param>
        /// <returns>Fiscal printer</returns>
        public static IFiscalPrinter Create(FiscalPrinterType type)
        {
            switch (type)
            {
                case FiscalPrinterType.TremolZfp:
                    return new Tremol.Zfp.TremolZfpFiscalPrinter();

                default:
                    throw new NotImplementedException();
            }
        }
    }
}