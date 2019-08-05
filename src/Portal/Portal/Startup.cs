using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddOptions<GeneralSettings>().Bind(Configuration.GetSection("General"));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            var dbConnection = new SqliteConnection("DataSource=:memory:");
            dbConnection.Open();

            services.AddEntityFrameworkSqlite()
                    .AddDbContext<WorkflowDbContext>((serviceProvider, options) => options.UseSqlite(dbConnection)
                    .UseInternalServiceProvider(serviceProvider));

            using (var sp = services.BuildServiceProvider())
            using (var context = sp.GetRequiredService<WorkflowDbContext>())
            {
                TasksDbBuilder.UsingDbContext(context)
                    .CreateTables()
                    .PopulateTables()
                    .SaveChanges();
            }
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
