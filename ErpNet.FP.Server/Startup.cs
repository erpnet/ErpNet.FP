namespace ErpNet.FP.Server
{
    using System.IO;
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
            app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

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

            // KeepAliveHostedService will warm up ServiceSingleton context at start
            services.AddHostedService<KeepAliveHostedService>();
        }

        public IConfiguration Configuration { get; }
    }
}
