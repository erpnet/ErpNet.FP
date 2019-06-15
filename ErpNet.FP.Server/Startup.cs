using ErpNet.FP.Core.Configuration;
using ErpNet.FP.Core.Service;
using ErpNet.FP.Server.Configuration;
using ErpNet.FP.Server.Contexts;
using ErpNet.FP.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ErpNet.FP.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureWritable<ServiceOptions>(Configuration.GetSection("ErpNet.FP"));
            services.AddSingleton<IServiceController, ServiceSingleton>();
            services.AddControllers().AddNewtonsoftJson();

            // KeepAliveHostedService will warm up PrintersController context at start
            services.AddHostedService<KeepAliveHostedService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            app.UseStaticFiles();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseMiddleware<ActionLoggingMiddleware>();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
