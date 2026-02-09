namespace ErpNet.FP.Server
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using ErpNet.FP.Core.Configuration;
    using ErpNet.FP.Core.Service;
    using ErpNet.FP.Server.Configuration;
    using ErpNet.FP.Server.Contexts;
    using ErpNet.FP.Server.Middlewares;
    using ErpNet.FP.Server.Services;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.StaticFiles;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Serilog;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDefaultFiles();

            // Set up custom content types - associating file extension to MIME type
            var customContentProvider = new FileExtensionContentTypeProvider();
            // Add new mappings
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
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "debug")
                ),
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

            app.UseRouting();
            app.UseCors("FrontendPolicy");

            app.Use(async (httpContext, next) =>
            {
                var serviceController = httpContext.RequestServices.GetRequiredService<IServiceController>();

                if (serviceController.WebAccess.EnablePrivateNetwork)
                {
                    httpContext.Response.OnStarting(() =>
                    {
                        httpContext.Response.Headers["Access-Control-Allow-Private-Network"] = "true";
                        return Task.CompletedTask;
                    });
                }
                await next();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

            services.ConfigureWritable<ServiceOptions>(Configuration.GetSection("ErpNet.FP"));

            services.AddSingleton<IServiceController, ServiceSingleton>();

            services.AddControllers().AddNewtonsoftJson();

            services.AddCors(options =>
            {
                options.AddPolicy("FrontendPolicy", builder =>
                {
                    var origins = new List<string>();
                    Configuration.GetSection("ErpNet.FP:WebAccess:AllowedOrigins").Bind(origins);

                    if (origins.Count == 0 || origins.Contains("*"))
                    {
                        builder.AllowAnyOrigin();
                    }
                    else
                    {
                        builder.WithOrigins(origins.ToArray());
                    }

                    builder.AllowAnyMethod().AllowAnyHeader();
                });
            });

            // KeepAliveHostedService will warm up ServiceSingleton context at start
            services.AddHostedService<KeepAliveHostedService>();
        }

        public IConfiguration Configuration { get; }
    }
}
