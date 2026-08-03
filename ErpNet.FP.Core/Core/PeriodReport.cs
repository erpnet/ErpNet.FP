namespace ErpNet.FP.Core
{
    using System;

    /// <summary>
    /// Represents a fiscal memory report for a custom period, which can be printed
    /// on a fiscal printer.
    /// </summary>
    public class PeriodReport : Credentials
    {
        /// <summary>
        /// The level of detail of the report. Defaults to a short (summary) report.
        /// </summary>
        public PeriodReportType Type { get; set; } = PeriodReportType.Short;

        /// <summary>
        /// The first date included in the report. Only the date part is used.
        /// Required - an omitted date is rejected with E405 by the validation, so that the
        /// caller gets a standard status response rather than a model binding failure.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The last date included in the report. Only the date part is used.
        /// Required - see <see cref="StartDate"/>.
        /// </summary>
        public DateTime EndDate { get; set; }
    }
}
