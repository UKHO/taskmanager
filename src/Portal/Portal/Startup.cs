using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using AutoMapper;
using Common.Factories;
using Common.Factories.Interfaces;
using Common.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Portal.Auth;
using Portal.Configuration;
using Portal.Helpers;
using Portal.HttpClients;
using Portal.MappingProfiles;
using WorkflowDatabase.EF;

namespace Portal
{
    public class Startup
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILogger<Startup> _logger;

        public Startup(IConfiguration configuration, IHostingEnvironment env, ILogger<Startup> logger)
        {
            _hostingEnvironment = env;
            _logger = logger;
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
                .Bind(Configuration.GetSection("azure"))
                .Bind(Configuration.GetSection("K2"));
            services.AddOptions<UriConfig>()
                .Bind(Configuration.GetSection("urls"));
            services.AddOptions<SecretsConfig>()
                .Bind(Configuration.GetSection("PortalSection"));
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            var isLocalDevelopment = ConfigHelpers.IsLocalDevelopment;

            var startupConfig = new StartupConfig();
            Configuration.GetSection("urls").Bind(startupConfig);
            Configuration.GetSection("databases").Bind(startupConfig);
            Configuration.GetSection("azure").Bind(startupConfig);
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

            // Use a singleton Microsoft.Graph.HttpProvider to avoid same issues HttpClient once suffered from
            services.AddSingleton<IHttpProvider, HttpProvider>();
            services.AddScoped<IPortalUser,
                PortalUser>(s => new PortalUser(s.GetService<IOptions<SecretsConfig>>(),
                s.GetService<IOptions<GeneralConfig>>(),
                isLocalDevelopment,
                s.GetService<HttpProvider>()));

            // Auto mapper config
            var mappingConfig = new MapperConfiguration(mc => { mc.AddProfile(new TaskViewModelMappingProfile()); });
            var mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);
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
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRequestLocalization();
            app.UseAzureAppConfiguration();

            app.UseAuthentication();

            app.UseMvc();
        }
    }
}
