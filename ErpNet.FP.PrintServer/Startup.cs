using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;

namespace ErpNet.FP.PrintServer
{
    internal class Startup
    {
        private readonly string DocXmlPath;
        private readonly string ApiTitle;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var assembly = typeof(Startup).Assembly;

            DocXmlPath = Path.ChangeExtension(assembly.Location, ".xml");
            var assemblyTitles = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute)).ToArray();
            if (assemblyTitles.Length > 0)
            {
                ApiTitle = ((AssemblyTitleAttribute)assemblyTitles[assemblyTitles.Length - 1]).Title;
            }
            else
            {
                ApiTitle = assembly.GetName().Name;
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(
                options=>
                {
                    options.RespectBrowserAcceptHeader = true;
                })
                .AddXmlSerializerFormatters()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = ApiTitle, Version = "v1" });

                var filePath = Path.Combine(System.AppContext.BaseDirectory, DocXmlPath);
                c.IncludeXmlComments(filePath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", ApiTitle);
            });
        }
    }
}
