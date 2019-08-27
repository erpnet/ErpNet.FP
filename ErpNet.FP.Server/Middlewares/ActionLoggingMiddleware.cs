using Microsoft.AspNetCore.Http;
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
            await LogRequest(context.Request);
            var bodyStream = context.Response.Body;
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;
                await requestDelegate(context);
                await LogResponse(context.Response);
                await responseBody.CopyToAsync(bodyStream);
            }
        }

        private async Task LogRequest(HttpRequest request)
        {
            try
            {
                request.EnableBuffering();
                var buffer = new byte[Convert.ToInt32(request.ContentLength)];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                var bodyAsText = Encoding.UTF8.GetString(buffer);
                request.Body.Position = 0;
                System.Diagnostics.Trace.WriteLine($"-- HTTP Request -- {request.Method}: {request.Scheme}://{request.Host}{request.Path}{request.QueryString}, Body({bodyAsText.Length}):{(bodyAsText.Length == 0 ? "" : Environment.NewLine)}{bodyAsText}");
            } 
            catch
            {
                System.Diagnostics.Trace.WriteLine($"-- HTTP Request -- {request.Method}: {request.Scheme}://{request.Host}{request.Path}{request.QueryString}");
            }
        }

        private async Task LogResponse(HttpResponse response)
        {
            try
            {
                response.Body.Seek(0, SeekOrigin.Begin);
                string bodyAsText = await new StreamReader(response.Body).ReadToEndAsync();
                response.Body.Seek(0, SeekOrigin.Begin);
                System.Diagnostics.Trace.WriteLine($"-- HTTP Response -- Code: {response.StatusCode}, Body({bodyAsText.Length}):{(bodyAsText.Length == 0 ? "" : Environment.NewLine)}{bodyAsText}");
            } 
            catch
            {
                System.Diagnostics.Trace.WriteLine($"-- HTTP Response -- Code: {response.StatusCode}");
            }
        }
    }
}
