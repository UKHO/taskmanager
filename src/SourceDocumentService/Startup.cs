using System;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SourceDocumentService.Config;
using SourceDocumentService.HttpClients;

namespace SourceDocumentService
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
            services.AddControllers();
            services.AddHealthChecks();

            services.AddScoped<IFileSystem, FileSystem>();

            var startupSecretsConfig = new StartupSecretsConfig();
            Configuration.GetSection("ContentService").Bind(startupSecretsConfig);

            services.AddHttpClient<IContentServiceApiClient, ContentServiceApiClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    Credentials = new NetworkCredential
                    {
                        UserName = startupSecretsConfig.ContentServiceUsername,
                        Password = startupSecretsConfig.ContentServicePassword,
                        Domain = startupSecretsConfig.ContentServiceDomain
                    }
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllers();
            });
        }
    }
}
