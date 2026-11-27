namespace ErpNet.FP.Core.Logging
{
    using System.Globalization;
    using System.IO;
    using Serilog.Events;
    using Serilog.Formatting;
    using Serilog.Formatting.Display;
    using CoreProvider = ErpNet.FP.Core.Provider.Provider;

    /// <summary>
    /// Renders log events with the given output template and masks device URI secrets (anything after the port),
    /// so that neither the console nor the file sink ever receives them.
    /// </summary>
    public sealed class RedactingTextFormatter : ITextFormatter
    {
        private readonly MessageTemplateTextFormatter inner;

        public RedactingTextFormatter(string outputTemplate)
        {
            inner = new MessageTemplateTextFormatter(outputTemplate, null);
        }

        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (!CoreProvider.MightContainDeviceUri(logEvent.RenderMessage())
                && !CoreProvider.MightContainDeviceUri(logEvent.Exception?.ToString()))
            {
                inner.Format(logEvent, output);
                return;
            }

            using var buffer = new StringWriter(CultureInfo.InvariantCulture);
            inner.Format(logEvent, buffer);
            output.Write(CoreProvider.RedactDeviceUrisInText(buffer.ToString()));
        }
    }
}
