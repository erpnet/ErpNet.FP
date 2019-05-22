using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ErpNet.FP.Server
{
    public class ActionLoggingMiddleware
    {
        private readonly RequestDelegate requestDelegate;

        public ActionLoggingMiddleware(RequestDelegate requestDelegate)
        {
            this.requestDelegate = requestDelegate;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = await FormatRequest(context.Request);

            // Log request string
            System.Diagnostics.Trace.WriteLine(request);

            var bodyStream = context.Response.Body;
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;
                await requestDelegate(context);
                var response = await FormatResponse(context.Response);

                // Log response string
                System.Diagnostics.Trace.WriteLine(response);

                await responseBody.CopyToAsync(bodyStream);
            }
        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            request.EnableRewind();
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            request.Body.Position = 0;

            return $"-- HTTP Request -- {request.Method}: {request.Scheme}:/{request.Host}{request.Path}{request.QueryString}, Body({bodyAsText.Length}):{(bodyAsText.Length == 0 ? "" : Environment.NewLine)}{bodyAsText}";
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            string bodyAsText = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return $"-- HTTP Response -- Code: {response.StatusCode}, Body({bodyAsText.Length}):{(bodyAsText.Length == 0 ? "" : Environment.NewLine)}{bodyAsText}";
        }
    }
}
