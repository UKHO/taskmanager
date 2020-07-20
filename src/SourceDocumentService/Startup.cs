using System;
using System.Configuration;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SourceDocumentService.Configuration;
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

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ADRoleOnly", policy => policy.RequireRole(ConfigurationManager.AppSettings["PermittedRoles"]));
            });

            services.AddMvc(config =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                config.Filters.Add(new AuthorizeFilter(policy));
            });

            services.AddScoped<IFileSystem, FileSystem>();
            services.AddScoped<IConfigurationManager, AppConfigConfigurationManager>();

            services.AddHttpClient<IContentServiceApiClient, ContentServiceApiClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    Credentials = new NetworkCredential
                    {
                        UserName = ConfigurationManager.AppSettings["ContentServiceUsername"],
                        Password = ConfigurationManager.AppSettings["ContentServicePassword"]
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
