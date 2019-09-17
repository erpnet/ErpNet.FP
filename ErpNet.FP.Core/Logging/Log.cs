using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ErpNet.FP.Core.Logging
{
    public static class Log
    {
        private static ILogger? _logger;

        public static void Information(string message)
        {
            if (_logger != null)
            {
                _logger.LogInformation(message);
            }
        }

        public static void Warning(string message)
        {
            if (_logger != null)
            {
                _logger.LogWarning(message);
            }
        }

        public static void Error(string message)
        {
            if (_logger != null)
            {
                _logger.LogError(message);
            }
        }

        public static void Setup(ILogger logger)
        {
            _logger = logger;
        }
    }
}
