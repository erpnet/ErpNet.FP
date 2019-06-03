using System.Collections.Generic;

namespace ErpNet.FP.Core
{
    /// <summary>
    /// Represents one Receipt, which can be printed on a fiscal printer.
    /// </summary>
    public class Credentials
    {
        /// <summary>
        /// Operator Name or Operator ID.
        /// </summary>
        public string Operator { get; set; } = string.Empty;

        /// <summary>
        /// Operator Password.
        /// </summary>
        public string OperatorPassword { get; set; } = string.Empty;
    }
}