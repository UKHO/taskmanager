using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using AutoMapper;
using Common.Factories;
using Common.Factories.Interfaces;
using Common.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Portal.Configuration;
using Portal.Helpers;
using Portal.HttpClients;
using Portal.MappingProfiles;
using WorkflowDatabase.EF;

namespace Portal
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
            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-GB");
                options.SupportedCultures = new List<CultureInfo> { new CultureInfo("en-GB"), new CultureInfo("en-GB") };
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddOptions<GeneralConfig>()
                .Bind(Configuration.GetSection("portal"))
                .Bind(Configuration.GetSection("apis"))
                .Bind(Configuration.GetSection("K2"));
            services.AddOptions<UriConfig>()
                .Bind(Configuration.GetSection("urls"));

            services.AddRazorPages().AddRazorRuntimeCompilation();

            var isLocalDevelopment = ConfigHelpers.IsLocalDevelopment;

            var startupConfig = new StartupConfig();
            Configuration.GetSection("urls").Bind(startupConfig);
            Configuration.GetSection("databases").Bind(startupConfig);

            var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDevelopment,
                isLocalDevelopment ? startupConfig.LocalDbServer : startupConfig.WorkflowDbServer, startupConfig.WorkflowDbName);

            services.AddDbContext<WorkflowDbContext>((serviceProvider, options) =>
                options.UseSqlServer(workflowDbConnectionString));

            if (isLocalDevelopment)
            {
                using (var sp = services.BuildServiceProvider())
                using (var context = sp.GetRequiredService<WorkflowDbContext>())
                {
                    TestWorkflowDatabaseSeeder.UsingDbContext(context).PopulateTables().SaveChanges();
                }
            }

            var startupSecretConfig = new StartupSecretsConfig();
            Configuration.GetSection("K2RestApi").Bind(startupSecretConfig);

            services.AddHttpClient<IDataServiceApiClient, DataServiceApiClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddHttpClient<IEventServiceApiClient, EventServiceApiClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddHttpClient<IWorkflowServiceApiClient, WorkflowServiceApiClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true,
                    Credentials = new NetworkCredential(startupSecretConfig.K2RestApiUsername, startupSecretConfig.K2RestApiPassword)
                });


            services.AddScoped<IDocumentStatusFactory, DocumentStatusFactory>();
            services.AddScoped<IOnHoldCalculator, OnHoldCalculator>();
            services.AddScoped<ICommentsHelper, CommentsHelper>();

            // Auto mapper config
            var mappingConfig = new MapperConfiguration(mc => { mc.AddProfile(new TaskViewModelMappingProfile()); });
            var mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);

            services.AddHealthChecks();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            // Ordered following rules for maintaining security at:
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.0#middleware-order

            app.UseHttpsRedirection();
            app.UseStaticFiles(); // must remain before UseRoutine()
            app.UseRouting();
            app.UseRequestLocalization();
            // app.UseCors() goes here if and when required

            // These will go in this position
            // app.UseAuthentication();
            // app.UseAuthorization()
            // app.UseSession() goes here if we want to maintain user sessions

            app.UseCookiePolicy();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHealthChecks("/health");
            });

            app.UseAzureAppConfiguration();
        }
    }
}
