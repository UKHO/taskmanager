using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using NCNEPortal.Auth;
using NCNEPortal.Configuration;


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

            services.AddOptions<GeneralConfig>()
                .Bind(Configuration.GetSection("portal"))
                .Bind(Configuration.GetSection("subscription"));
            services.AddOptions<UriConfig>()
                .Bind(Configuration.GetSection("urls"));
            services.AddOptions<SecretsConfig>()
                .Bind(Configuration.GetSection("PortalSection"));

            services.AddRazorPages();

            services.AddHealthChecks();

            var startupConfig = new StartupConfig();
            Configuration.GetSection("urls").Bind(startupConfig);
            Configuration.GetSection("databases").Bind(startupConfig);
            Configuration.GetSection("subscription").Bind(startupConfig);
            Configuration.GetSection("portal").Bind(startupConfig);


            // Use a singleton Microsoft.Graph.HttpProvider to avoid same issues HttpClient once suffered from
            services.AddSingleton<IHttpProvider, HttpProvider>();
            services.AddScoped<IUserIdentityService,
                UserIdentityService>(s => new UserIdentityService(s.GetService<IOptions<SecretsConfig>>(),
                s.GetService<IOptions<GeneralConfig>>(),
                s.GetService<IOptions<UriConfig>>(),
                isLocalDevelopment,
                s.GetService<HttpProvider>()));

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
