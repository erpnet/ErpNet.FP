namespace ErpNet.FP.Core
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// The level of detail of a fiscal memory report for a period.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PeriodReportType
    {
        /// <summary>
        /// Summary report - only the accumulated totals for the whole period.
        /// </summary>
        [EnumMember(Value = "short")]
        Short = 0,

        /// <summary>
        /// Detailed report - every daily (Z) report stored in the fiscal memory for the period.
        /// </summary>
        [EnumMember(Value = "detailed")]
        Detailed = 1
    }
}
