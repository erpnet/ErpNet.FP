namespace ErpNet.FP.Server.Middlewares
{
    using System;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Serilog;

    public class ActionLoggingMiddleware
    {
        private readonly RequestDelegate requestDelegate;

        public ActionLoggingMiddleware(RequestDelegate requestDelegate)
        {
            this.requestDelegate = requestDelegate;
        }

        public async Task Invoke(HttpContext context)
        {
            await LogRequest(context.Request);
            var bodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;
            await requestDelegate(context);
            await LogResponse(context.Response, context.Request.Path);
            await responseBody.CopyToAsync(bodyStream);
        }

        private async Task LogRequest(HttpRequest request)
        {
            var configurationPath = IsPrinterConfigurationPath(request.Path.Value);
            var isConfigurationWrite = IsPrinterConfigurationWritePath(request.Path.Value);
            var requestUri = $"{request.Scheme}://{request.Host}{request.Path}{SafeQueryString(request.QueryString.ToString(), configurationPath)}";
            try
            {
                request.EnableBuffering();
                var buffer = new byte[Convert.ToInt32(request.ContentLength)];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                var bodyAsText = Encoding.UTF8.GetString(buffer);
                request.Body.Position = 0;
                var bodyForLog = RedactSensitiveUris(bodyAsText, isConfigurationWrite);
                if (bodyForLog == null)
                {
                    Log.Information($"-- HTTP Request -- {request.Method}: {requestUri}, Body omitted because the URI could not be safely redacted.");
                    return;
                }
                Log.Information($"-- HTTP Request -- {request.Method}: {requestUri}, Body({bodyForLog.Length}):{(bodyForLog.Length == 0 ? string.Empty : Environment.NewLine)}{bodyForLog}");
            }
            catch
            {
                Log.Information($"-- HTTP Request -- {request.Method}: {requestUri}");
            }
        }

        private async Task LogResponse(HttpResponse response, PathString requestPath)
        {
            try
            {
                response.Body.Seek(0, SeekOrigin.Begin);
                var streamReader = new StreamReader(response.Body);
                string bodyAsText = await streamReader.ReadToEndAsync();
                response.Body.Seek(0, SeekOrigin.Begin);
                var bodyForLog = RedactSensitiveUris(bodyAsText, IsPrinterConfigurationPath(requestPath.Value));
                if (bodyForLog == null)
                {
                    Log.Information($"-- HTTP Response -- Code: {response.StatusCode}, Body omitted because the URI could not be safely redacted.");
                    return;
                }
                Log.Information($"-- HTTP Response -- Code: {response.StatusCode}, Body({bodyForLog.Length}):{(bodyForLog.Length == 0 ? string.Empty : Environment.NewLine)}{bodyForLog}");
            }
            catch
            {
                Log.Information($"-- HTTP Response -- Code: {response.StatusCode}");
            }
        }

        private static bool IsPrinterConfigurationPath(string? path)
        {
            var normalizedPath = path?.TrimEnd('/');
            return string.Equals(normalizedPath, "/service/printers", StringComparison.OrdinalIgnoreCase)
                || IsPrinterConfigurationWritePath(normalizedPath);
        }

        private static bool IsPrinterConfigurationWritePath(string? path)
        {
            var normalizedPath = path?.TrimEnd('/');
            return string.Equals(normalizedPath, "/service/printers/configure", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedPath, "/service/printers/update", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedPath, "/service/printers/delete", StringComparison.OrdinalIgnoreCase);
        }

        private static string SafeQueryString(string queryString, bool omitQuery)
        {
            if (omitQuery)
            {
                return string.Empty;
            }

            try
            {
                return MightContainDeviceUriSecret(Uri.UnescapeDataString(queryString))
                    ? string.Empty
                    : queryString;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool MightContainDeviceUriSecret(string value)
        {
            if (global::ErpNet.FP.Core.Provider.Provider.MightContainDeviceUriSecret(value))
            {
                return true;
            }

            try
            {
                return global::ErpNet.FP.Core.Provider.Provider.MightContainDeviceUriSecret(
                    Uri.UnescapeDataString(value));
            }
            catch
            {
                return true;
            }
        }

        private static string? RedactSensitiveUris(string body, bool inspectJson)
        {
            if (body.Length == 0)
            {
                return body;
            }

            if (!inspectJson && !MightContainDeviceUriSecret(body))
            {
                return body;
            }

            try
            {
                var token = JToken.Parse(body);
                if (!(token is JContainer container))
                {
                    return null;
                }

                foreach (var value in container.Descendants().OfType<JValue>())
                {
                    if (value.Type != JTokenType.String)
                    {
                        continue;
                    }

                    var uri = value.Value as string;
                    if (uri == null || !MightContainDeviceUriSecret(uri))
                    {
                        continue;
                    }

                    if (!global::ErpNet.FP.Core.Provider.Provider.HasDeviceUriSecret(uri))
                    {
                        return null;
                    }

                    value.Value = global::ErpNet.FP.Core.Provider.Provider.RedactDeviceUri(uri);
                }

                return token.ToString(Formatting.None);
            }
            catch
            {
                return null;
            }
        }
    }
}
