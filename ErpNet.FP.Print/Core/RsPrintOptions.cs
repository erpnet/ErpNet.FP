namespace ErpNet.FP.Core
{
    /// <summary>
    /// Print options for RS232
    /// </summary>
    /// <seealso cref="ErpNet.FP.Core.PrintOptions" />
    public class RsPrintOptions : PrintOptions
    {
        /// <summary>
        /// Gets or sets the baud rate.
        /// </summary>
        /// <value>
        /// The baud rate.
        /// </value>
        public int BaudRate { get; set; }

    }
}
