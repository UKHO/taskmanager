using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using AutoMapper;
using Common.Factories;
using Common.Factories.Interfaces;
using Common.Helpers;
using HpdDatabase.EF.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Portal.Auth;
using WorkflowDatabase.EF.Models;

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

            services.AddOptions<GeneralConfig>()
                .Bind(Configuration.GetSection("portal"))
                .Bind(Configuration.GetSection("apis"))
                .Bind(Configuration.GetSection("subscription"))
                .Bind(Configuration.GetSection("K2"));
            services.AddOptions<UriConfig>()
                .Bind(Configuration.GetSection("urls"));
            services.AddOptions<SecretsConfig>()
                .Bind(Configuration.GetSection("PortalSection"));

            services.AddRazorPages().AddRazorRuntimeCompilation();

            var isLocalDevelopment = ConfigHelpers.IsLocalDevelopment;

            var startupConfig = new StartupConfig();
            Configuration.GetSection("urls").Bind(startupConfig);
            Configuration.GetSection("databases").Bind(startupConfig);
            Configuration.GetSection("subscription").Bind(startupConfig);
            Configuration.GetSection("portal").Bind(startupConfig);

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
            Configuration.GetSection("HpdDbSection").Bind(startupSecretConfig);

            var hpdConnection = DatabasesHelpers.BuildOracleConnectionString(startupSecretConfig.DataSource,
                startupSecretConfig.UserId, startupSecretConfig.Password);

            services.AddDbContext<HpdDbContext>((serviceProvider, options) =>
                options.UseOracle(hpdConnection));

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

            services.AddScoped<IDocumentStatusFactory, DocumentStatusFactory>();
            services.AddScoped<IOnHoldCalculator, OnHoldCalculator>();
            services.AddScoped<ICommentsHelper, CommentsHelper>();
            services.AddScoped<ITaskDataHelper, TaskDataHelper>();

            // Use a singleton Microsoft.Graph.HttpProvider to avoid same issues HttpClient once suffered from
            services.AddSingleton<IHttpProvider, HttpProvider>();
            services.AddScoped<IUserIdentityService,
                UserIdentityService>(s => new UserIdentityService(s.GetService<IOptions<SecretsConfig>>(),
                s.GetService<IOptions<GeneralConfig>>(),
                s.GetService<IOptions<UriConfig>>(),
                isLocalDevelopment,
                s.GetService<HttpProvider>()));

            // Auto mapper config
            var mappingConfig = new MapperConfiguration(mc => { mc.AddProfile(new TaskViewModelMappingProfile()); });
            var mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);

            services.AddHealthChecks();

            using (var sp = services.BuildServiceProvider())
            using (var workflowDbContext = sp.GetRequiredService<WorkflowDbContext>())
            using (var hpdDbContext = sp.GetRequiredService<HpdDbContext>())
            {
                try
                {
                    var workspaces = hpdDbContext.CarisWorkspaces
                        .Select(cw => cw.Name.Trim()) //trim to prevent unique constraint errors
                        .Distinct()
                        .Select(cw => new CachedHpdWorkspace { Name = cw })
                        .OrderBy(cw => cw.Name)
                        .ToList();

                    workflowDbContext.Database.ExecuteSqlCommand("Truncate Table [CachedHpdWorkspace]");

                    workflowDbContext.CachedHpdWorkspace.AddRange(workspaces);
                    workflowDbContext.SaveChanges();
                }
                catch (Exception e)
                {
                    //TODO: LOG!
                }

            }
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
        }
    }
}
