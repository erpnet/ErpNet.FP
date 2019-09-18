using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ErpNet.FP.Core.Logging
{
    public static class Log
    {
        private static ILogger? Logger;
        private static TextWriter? LogWriter;
        private static Timer? Timer;

        private static string FormatLogMessage(string message)
        {
            var timeStamp = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff", CultureInfo.InvariantCulture);
            return $"[{timeStamp}] {message}";
        }

        public static void Information(string message)
        {
            if (Logger != null)
            {
                Logger.LogInformation(FormatLogMessage(message));
            } 
            if (LogWriter != null)
            {
                LogWriter.WriteLine($"info: {FormatLogMessage(message)}");
            }
        }

        public static void Warning(string message)
        {
            if (Logger != null)
            {
                Logger.LogWarning(FormatLogMessage(message));
            }
            if (LogWriter != null)
            {
                LogWriter.WriteLine($"warn: {FormatLogMessage(message)}");
            }
        }

        public static void Error(string message)
        {
            if (Logger != null)
            {
                Logger.LogError(FormatLogMessage(message));
            }
            if (LogWriter != null)
            {
                LogWriter.WriteLine($"fail: {FormatLogMessage(message)}");
            }
        }

        public static void Setup(ILogger logger)
        {
            Logger = logger;
        }

        private static void AutoFlushLogWriter(object state)
        {
            if (LogWriter != null)
            {
                LogWriter.Flush();
            }
        }

        public static void Setup(TextWriter logWriter)
        {
            Timer = new Timer(AutoFlushLogWriter, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            LogWriter = logWriter;
        }
    }
}
