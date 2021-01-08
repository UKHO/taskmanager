using System;
using Common.Helpers;
using Common.Helpers.Auth;
using DbUpdatePortal.Auth;
using DbUpdatePortal.Configuration;
using DbUpdatePortal.Helpers;
using DbUpdatePortal.HostedServices;
using DbUpdateWorkflowDatabase.EF;
using HpdDatabase.EF.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Serilog;
using Serilog.Events;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

namespace DbUpdatePortal
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
            Configuration.GetSection("LoggingDbSection").Bind(startupSecretsConfig);

            LoggingHelper.SetupLogging(isLocalDevelopment, startupLoggingConfig, startupSecretsConfig);

            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-GB");
                options.SupportedCultures = new List<CultureInfo> { new CultureInfo("en-GB"), new CultureInfo("en-GB") };
            });

            services.AddOptions<GeneralConfig>()
                .Bind(Configuration.GetSection("dbupdateportal"))
                .Bind(Configuration.GetSection("subscription"))
                .Bind(Configuration.GetSection("caris"));
            services.AddOptions<UriConfig>()
                .Bind(Configuration.GetSection("urls"));
            services.AddOptions<StartupSecretsConfig>()
                .Bind(Configuration.GetSection("DbUpdatePortalSection"));

            services.AddRazorPages();
            services.AddHealthChecks();

            var startupConfig = new StartupConfig();
            Configuration.GetSection("urls").Bind(startupConfig);
            Configuration.GetSection("databases").Bind(startupConfig);
            Configuration.GetSection("subscription").Bind(startupConfig);
            Configuration.GetSection("dbupdateportal").Bind(startupConfig);

            services.AddOptions<AdUserUpdateServiceConfig>()
                .Bind(Configuration.GetSection("dbupdateportal"));
            services.AddOptions<AdUserUpdateServiceSecrets>()
                .Bind(Configuration.GetSection("DbUpdateActiveDirectory"));

            // Use a singleton Microsoft.Graph.HttpProvider to avoid same issues HttpClient once suffered from
            services.AddSingleton<IHttpProvider, HttpProvider>();


            // TODO - refactor all GetService away
            services.AddSingleton<IAdDirectoryService,
                AdDirectoryService>(s => new AdDirectoryService(
                s.GetService<IOptions<StartupSecretsConfig>>().Value.ClientAzureAdSecret,
                s.GetService<IOptions<GeneralConfig>>().Value.AzureAdClientId,
                s.GetService<IOptions<GeneralConfig>>().Value.TenantId,
                isLocalDevelopment
                    ? s.GetService<IOptions<UriConfig>>().Value.DbUpdateLocalDevLandingPageHttpsUrl
                    : s.GetService<IOptions<UriConfig>>().Value.DbUpdateLandingPageUrl,
                s.GetService<HttpProvider>()));

            services.AddSingleton<AppVersionInfo>();

            services.AddMicrosoftIdentityWebAppAuthentication(Configuration, "DbUpdatePortalAzureAd");

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

            services.AddScoped<IDbUpdateUserDbService,
                DbUpdateUserDbService>(s => new DbUpdateUserDbService(s.GetService<DbUpdateWorkflowDbContext>(), s.GetService<IAdDirectoryService>()));

            services.AddScoped<IDbUpdateUserDbService, DbUpdateUserDbService>();
            services.AddScoped<ICarisProjectHelper, CarisProjectHelper>();
            services.AddScoped<IStageTypeFactory, StageTypeFactory>();
            services.AddScoped<IPageValidationHelper, PageValidationHelper>();
            services.AddScoped<ICommentsHelper, CommentsHelper>();

            // Order of these two is important
            if (ConfigHelpers.IsLocalDevelopment || ConfigHelpers.IsAzureUat)
                services.AddHostedService<DatabaseSeedingService>();
            services.AddHostedService<AdUserUpdateService>();

            var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDevelopment,
                isLocalDevelopment ? startupConfig.LocalDbServer : startupConfig.WorkflowDbServer, startupConfig.DbUpdateWorkflowDbName);

            services.AddDbContext<DbUpdateWorkflowDbContext>((serviceProvider, options) =>
                options.UseSqlServer(workflowDbConnectionString));

            var startupSecretConfig = new StartupSecretsConfig();
            Configuration.GetSection("K2RestApi").Bind(startupSecretConfig);
            Configuration.GetSection("HpdDbSection").Bind(startupSecretConfig);

            var hpdConnection = DatabasesHelpers.BuildOracleConnectionString(startupSecretConfig.DataSource,
                startupSecretConfig.UserId, startupSecretConfig.Password);

            services.AddDbContext<HpdDbContext>((serviceProvider, options) =>
                options.UseOracle(hpdConnection));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseRequestLocalization();

            app.UseAuthentication();
            app.UseAuthorization();

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
