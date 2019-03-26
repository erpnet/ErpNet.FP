namespace ErpNet.FP.Print.Core
{
    /// <summary>
    /// Information returned after printing.
    /// </summary>
    public class PrintInfo
    {
        /// <summary>
        /// Gets or sets the fiscal memory position.
        /// </summary>
        /// <value>
        /// The fiscal memory position.
        /// </value>
        public string FiscalMemoryPosition { get; }
        public string[] Statuses { get; }
        public string[] Warnings { get; }
        public string[] Errors { get; }
    }
}
