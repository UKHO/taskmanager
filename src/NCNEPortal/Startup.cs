using System;
using System.Collections.Generic;
using System.Globalization;
using Common.Helpers;
using Common.Helpers.Auth;
using HpdDatabase.EF.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using NCNEPortal.Auth;
using NCNEPortal.Calculators;
using NCNEPortal.Configuration;
using NCNEPortal.Helpers;
using NCNEWorkflowDatabase.EF;
using Serilog;
using Serilog.Events;


namespace NCNEPortal
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
                .Bind(Configuration.GetSection("ncneportal"))
                .Bind(Configuration.GetSection("subscription"))
                .Bind(Configuration.GetSection("caris")); ;
            services.AddOptions<UriConfig>()
                .Bind(Configuration.GetSection("urls"));
            services.AddOptions<SecretsConfig>()
                .Bind(Configuration.GetSection("NcnePortalSection"))
                .Bind(Configuration.GetSection("NcneActiveDirectory"));

            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddHealthChecks();

            var startupConfig = new StartupConfig();
            Configuration.GetSection("urls").Bind(startupConfig);
            Configuration.GetSection("databases").Bind(startupConfig);
            Configuration.GetSection("subscription").Bind(startupConfig);
            Configuration.GetSection("ncneportal").Bind(startupConfig);

            // Use a singleton Microsoft.Graph.HttpProvider to avoid same issues HttpClient once suffered from
            services.AddSingleton<IHttpProvider, HttpProvider>();

            services.AddScoped<IAdDirectoryService,
                AdDirectoryService>(s => new AdDirectoryService(
                s.GetService<IOptions<SecretsConfig>>().Value.ClientAzureAdSecret,
                s.GetService<IOptions<GeneralConfig>>().Value.AzureAdClientId,
                s.GetService<IOptions<GeneralConfig>>().Value.TenantId,
                isLocalDevelopment
                    ? s.GetService<IOptions<UriConfig>>().Value.NcneLocalDevLandingPageHttpsUrl
                    : s.GetService<IOptions<UriConfig>>().Value.NcneLandingPageUrl,
                s.GetService<HttpProvider>()));

            services.AddScoped<INcneUserDbService,
                NcneUserDbService>(s => new NcneUserDbService(s.GetService<NcneWorkflowDbContext>(), s.GetService<IAdDirectoryService>()));

            services.AddScoped<IMilestoneCalculator, MilestoneCalculator>();
            services.AddScoped<ICommentsHelper, CommentsHelper>();
            services.AddScoped<ICarisProjectHelper, CarisProjectHelper>();
            services.AddScoped<IPageValidationHelper, PageValidationHelper>();
            services.AddScoped<IStageTypeFactory, StageTypeFactory>();
            services.AddScoped<IWorkflowStageHelper, WorkflowStageHelper>();

            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                .AddAzureAD(options =>
                {
                    options.ClientId = startupConfig.AzureAdClientId;
                    options.Instance = "https://ukho.onmicrosoft.com";
                    options.CallbackPath = "/signin-oidc";
                    options.TenantId = startupConfig.TenantId;
                    options.Domain = "ukho.onmicrosoft.com";

                });

            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, options =>
            {
                options.Authority = $"https://login.microsoftonline.com/{startupConfig.TenantId}/v2.0/";
                options.TokenValidationParameters.ValidateIssuer = false; // accept several tenants (here simplified for development - TODO)
            });

            var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDevelopment,
                    isLocalDevelopment ? startupConfig.LocalDbServer : startupConfig.WorkflowDbServer, startupConfig.NcneWorkflowDbName);

            services.AddDbContext<NcneWorkflowDbContext>((serviceProvider, options) =>
                options.UseSqlServer(workflowDbConnectionString));

            var startupSecretConfig = new StartupSecretsConfig();
            Configuration.GetSection("K2RestApi").Bind(startupSecretConfig);
            Configuration.GetSection("HpdDbSection").Bind(startupSecretConfig);

            var hpdConnection = DatabasesHelpers.BuildOracleConnectionString(startupSecretConfig.DataSource,
                startupSecretConfig.UserId, startupSecretConfig.Password);

            services.AddDbContext<HpdDbContext>((serviceProvider, options) =>
                options.UseOracle(hpdConnection));

            services.AddSingleton<AppVersionInfo>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
            IWebHostEnvironment env,
            IOptions<SecretsConfig> secrets,
            INcneUserDbService userDbService,
            NcneWorkflowDbContext ncneWorkflowDbContext)
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
            });

            app.UseAzureAppConfiguration();

            // Seeding

            if (ConfigHelpers.IsLocalDevelopment || ConfigHelpers.IsAzureUat)
                SeedWorkflowDatabase(ncneWorkflowDbContext);

            UpdateDbFromAd(secrets.Value, userDbService);
        }

        private static void SeedWorkflowDatabase(NcneWorkflowDbContext ncneWorkflowDbContext)
        {
            using var context = ncneWorkflowDbContext;
            NcneTestWorkflowDatabaseSeeder.UsingDbContext(context).PopulateTables().SaveChanges();
            Log.Logger.Information($"NCNEWorkflowDatabase successfully re-seeded.");
        }

        private static void UpdateDbFromAd(SecretsConfig secrets, INcneUserDbService userDbService)
        {
            var adGroupGuids = new List<Guid> { secrets.NcGuid, secrets.NeGuid };

            try
            {
                userDbService.UpdateDbFromAdAsync(adGroupGuids).Wait();
                Log.Logger.Information($"Users successfully updated in database from AD for guids");
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Startup error: Failed to update users from AD.");
            }
        }
    }
}
