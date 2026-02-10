namespace ErpNet.FP.Server
{
    using System.IO;
    using System.Linq;
    using ErpNet.FP.Core.Configuration;
    using ErpNet.FP.Core.Service;
    using ErpNet.FP.Server.Configuration;
    using ErpNet.FP.Server.Contexts;
    using ErpNet.FP.Server.Middlewares;
    using ErpNet.FP.Server.Services;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.StaticFiles;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
    using Serilog;

    public class Startup
    {
        private readonly WebAccessOptions _webAccessOptions;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            _webAccessOptions = Configuration.GetSection("ErpNet.FP:WebAccess").Get<WebAccessOptions>()
                ?? new WebAccessOptions();
        }

        public IConfiguration Configuration { get; }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDefaultFiles();

            var customContentProvider = new FileExtensionContentTypeProvider();
            customContentProvider.Mappings[".log"] = "text/plain; charset=UTF-8";

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
                ContentTypeProvider = customContentProvider
            });

            app.UseDirectoryBrowser(new DirectoryBrowserOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "debug")),
                RequestPath = "/debug"
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseSerilogRequestLogging();
            app.UseMiddleware<ActionLoggingMiddleware>();

            if (_webAccessOptions.EnablePrivateNetwork)
            {
                app.Use(async (context, next) =>
                {
                    // This header enables public websites to talk to local devices (Chrome 94+).
                    context.Response.Headers.Append("Access-Control-Allow-Private-Network", "true");
                    await next();
                });
            }

            app.UseRouting();

            app.UseCors("FrontendPolicy");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

            services.ConfigureWritable<ServiceOptions>(Configuration.GetSection("ErpNet.FP"));

            services.AddSingleton<IServiceController, ServiceSingleton>();

            services.AddControllers().AddNewtonsoftJson();

            services.AddCors(options =>
            {
                options.AddPolicy("FrontendPolicy", builder =>
                {
                    var origins = _webAccessOptions.AllowedOrigins;

                    if (origins == null || origins.Count == 0 || origins.Contains("*"))
                    {
                        builder.SetIsOriginAllowed(_ => true);
                    }
                    else
                    {
                        builder.WithOrigins(origins.ToArray());
                    }

                    builder
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            services.AddHostedService<KeepAliveHostedService>();
        }
    }
}