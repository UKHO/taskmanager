using System.Data.SqlClient;
using System.Diagnostics;
using AutoMapper;
using Common.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portal.Configuration;
using Portal.MappingProfiles;
using WorkflowDatabase.EF;

namespace Portal
{
    public class Startup
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            _hostingEnvironment = env;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddOptions<GeneralSettings>().Bind(Configuration.GetSection("General"));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            var isLocalDebugging = _hostingEnvironment.IsDevelopment() && Debugger.IsAttached;

            var startupConfig = new StartupConfig();
            Configuration.GetSection("urls").Bind(startupConfig);
            Configuration.GetSection("databases").Bind(startupConfig);

            var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDebugging,
                isLocalDebugging ? startupConfig.LocalDbServer : startupConfig.WorkflowDbServer, startupConfig.WorkflowDbName);

            var connection = new SqlConnection(workflowDbConnectionString)
            {
                AccessToken = isLocalDebugging ? 
                    null :
                    new AzureServiceTokenProvider().GetAccessTokenAsync(startupConfig.AzureDbTokenUrl.ToString()).Result
            };
            services.AddDbContext<WorkflowDbContext>((serviceProvider, options) =>
                options.UseSqlServer(connection));

            if (isLocalDebugging)
            {
                using (var sp = services.BuildServiceProvider())
                using (var context = sp.GetRequiredService<WorkflowDbContext>())
                {
                    TasksDbBuilder.UsingDbContext(context).PopulateTables().SaveChanges();
                }
            }
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

            app.UseMvc();
        }
    }
}
