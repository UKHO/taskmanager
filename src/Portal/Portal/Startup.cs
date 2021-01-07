using System;
using System.Collections.Generic;
using System.Globalization;
using AutoMapper;
using Common.Factories.DocumentStatusFactory;
using Common.Factories.Interfaces;
using Common.Helpers;
using Common.Helpers.Auth;
using HpdDatabase.EF.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Portal.Auth;
using Portal.BusinessLogic;
using Portal.Calculators;
using Portal.Configuration;
using Portal.Helpers;
using Portal.HostedServices;
using Portal.HttpClients;
using Portal.Hubs;
using Portal.MappingProfiles;
using Serilog;
using Serilog.Events;
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
            var isLocalDevelopment = ConfigHelpers.IsLocalDevelopment;
            var startupLoggingConfig = new StartupLoggingConfig();
            Configuration.GetSection("logging").Bind(startupLoggingConfig);

            var startupSecretsConfig = new StartupSecretsConfig();
            Configuration.GetSection("NsbDbSection").Bind(startupSecretsConfig);
            Configuration.GetSection("LoggingDbSection").Bind(startupSecretsConfig);
            Configuration.GetSection("PortalSection").Bind(startupSecretsConfig);
            Configuration.GetSection("K2RestApi").Bind(startupSecretsConfig);
            Configuration.GetSection("HpdDbSection").Bind(startupSecretsConfig);
            Configuration.GetSection("PortalActiveDirectory").Bind(startupSecretsConfig);

            services.AddOptions<StartupConfig>()
                .Bind(Configuration.GetSection("urls"))
                .Bind(Configuration.GetSection("databases"))
                .Bind(Configuration.GetSection("subscription"))
                .Bind(Configuration.GetSection("portal"));

            services.AddOptions<AdUserUpdateServiceConfig>()
                .Bind(Configuration.GetSection("portal"));

            services.AddOptions<HpdEncProductUpdateServiceConfig>()
                .Bind(Configuration.GetSection("portal"));

            services.AddOptions<HpdWorkspaceUpdateServiceConfig>()
                .Bind(Configuration.GetSection("portal"));

            services.AddOptions<GeneralConfig>()
                .Bind(Configuration.GetSection("portal"))
                .Bind(Configuration.GetSection("apis"))
                .Bind(Configuration.GetSection("subscription"))
                .Bind(Configuration.GetSection("K2"))
                .Bind(Configuration.GetSection("caris"));

            services.AddOptions<UriConfig>()
                .Bind(Configuration.GetSection("urls"));

            services.AddOptions<SecretsConfig>()
                .Bind(Configuration.GetSection("HpdDbSection"));

            services.AddOptions<SqlListenerSecretsConfig>()
                .Bind(Configuration.GetSection("SqlListener"));

            services.AddOptions<StartupSecretsConfig>()
                .Bind(Configuration.GetSection("NsbDbSection"))
                .Bind(Configuration.GetSection("LoggingDbSection"))
                .Bind(Configuration.GetSection("PortalSection"))
                .Bind(Configuration.GetSection("K2RestApi"))
                .Bind(Configuration.GetSection("HpdDbSection"))
                .Bind(Configuration.GetSection("PCPEventService"));

            services.AddOptions<AdUserUpdateServiceSecrets>()
                .Bind(Configuration.GetSection("PortalActiveDirectory"));

            LoggingHelper.SetupLogging(isLocalDevelopment, startupLoggingConfig, startupSecretsConfig);

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


            services.AddSignalR();

            var startupConfig = new StartupConfig();
            Configuration.GetSection("urls").Bind(startupConfig);
            Configuration.GetSection("databases").Bind(startupConfig);
            Configuration.GetSection("subscription").Bind(startupConfig);
            Configuration.GetSection("portal").Bind(startupConfig);

            var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDevelopment,
                isLocalDevelopment ? startupConfig.LocalDbServer : startupConfig.WorkflowDbServer, startupConfig.WorkflowDbName);

            services.AddDbContext<WorkflowDbContext>((serviceProvider, options) =>
                options.UseLazyLoadingProxies().UseSqlServer(workflowDbConnectionString));

            var hpdConnection = DatabasesHelpers.BuildOracleConnectionString(startupSecretsConfig.DataSource,
                startupSecretsConfig.UserId, startupSecretsConfig.Password);

            services.AddDbContext<HpdDbContext>((serviceProvider, options) =>
                options.UseOracle(hpdConnection));

            services.AddHttpClient<IDataServiceApiClient, DataServiceApiClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddHttpClient<IEventServiceApiClient, EventServiceApiClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddScoped<IDocumentStatusFactory, DocumentStatusFactory>();
            services.AddScoped<IOnHoldCalculator, OnHoldCalculator>();
            services.AddScoped<IDmEndDateCalculator, DmEndDateCalculator>();
            services.AddScoped<IIndexFacade, IndexFacade>();
            services.AddScoped<ICommentsHelper, CommentsHelper>();
            services.AddScoped<ITaskDataHelper, TaskDataHelper>();
            services.AddScoped<IPageValidationHelper, PageValidationHelper>();
            services.AddScoped<ISessionFileGenerator, SessionFileGenerator>();
            services.AddScoped<ICarisProjectHelper, CarisProjectHelper>();
            services.AddScoped<ICarisProjectNameGenerator, CarisProjectNameGenerator>();
            services.AddScoped<IWorkflowBusinessLogicService, WorkflowBusinessLogicService>();
            services.AddScoped<IPortalUserDbService, PortalUserDbService>();

            // Use a singleton Microsoft.Graph.HttpProvider to avoid same issues HttpClient once suffered from
            services.AddSingleton<IHttpProvider, HttpProvider>();
            //TODO refactor 
            services.AddSingleton<IAdDirectoryService,
                AdDirectoryService>(s => new AdDirectoryService(
                startupSecretsConfig.ClientAzureAdSecret,
                s.GetService<IOptions<GeneralConfig>>().Value.AzureAdClientId,
                s.GetService<IOptions<GeneralConfig>>().Value.TenantId,
                isLocalDevelopment
                    ? s.GetService<IOptions<UriConfig>>().Value.LocalDevLandingPageHttpsUrl
                    : s.GetService<IOptions<UriConfig>>().Value.LandingPageUrl,
                s.GetService<HttpProvider>()));

            // Order of these two is important
            if (ConfigHelpers.IsLocalDevelopment || ConfigHelpers.IsAzureUat)
                services.AddHostedService<DatabaseSeedingService>();
            services.AddHostedService<AdUserUpdateService>();
            services.AddHostedService<HpdEncProductUpdateService>();
            services.AddHostedService<HpdWorkspaceUpdateService>();
            services.AddHostedService<SqlListenerService>();

            // Auto mapper config
            var mappingConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new TaskViewModelMappingProfile());
                mc.AddProfile(new HistoricalTasksDataMappingProfile());
            });
            var mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);

            services.AddHealthChecks();

            services.AddSingleton<AppVersionInfo>();

            services.AddMicrosoftIdentityWebAppAuthentication(Configuration, "PortalAzureAd");

            services.AddRazorPages().AddRazorRuntimeCompilation().AddMvcOptions(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddMicrosoftIdentityUI();

            services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(startupConfig.CookieTimeoutHours);
                options.SlidingExpiration = true;

            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, WorkflowDbContext workflowDbContext,
            HpdDbContext hpdDbContext)
        {
            app.UseSerilogRequestLogging(
                options =>
                    options.GetLevel = (ctx, d, ex) =>
                    {
                        if (ex == null && ctx.Response.StatusCode <= 499)
                        {
                            return LogEventLevel.Verbose;
                        }

                        return LogEventLevel.Error;
                    }
            );

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
            app.UseStaticFiles(); // must remain before UseRouting()
            app.UseRouting();
            app.UseRequestLocalization();
            // app.UseCors() goes here if and when required

            app.UseAuthentication();
            app.UseAuthorization();
            // app.UseSession() goes here if we want to maintain user sessions

            app.UseCookiePolicy();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHealthChecks("/health");
                endpoints.MapHub<TasksHub>("/tasksupdate");
            });

            app.UseAzureAppConfiguration();

        }

    }
}
