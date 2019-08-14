namespace ErpNet.FP.Core
{
    /// <summary>
    /// RawRequest 
    /// </summary>
    public class RequestFrame : FiscalTask
    {
        /// <summary>
        /// The Raw request, including the command prefix
        /// </summary>
        public string RawRequest { get; set; } = string.Empty;
    }
}