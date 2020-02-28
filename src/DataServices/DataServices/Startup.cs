using System.IO;
using System.Linq;
using Common.Helpers;
using DataServices.Adapters;
using DataServices.Config;
using DataServices.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

namespace DataServices
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var isLocalDb = ConfigHelpers.IsLocalDevelopment;
            var startupLoggingConfig = new StartupLoggingConfig();
            Configuration.GetSection("logging").Bind(startupLoggingConfig);

            services.AddOptions<Settings>().Bind(Configuration.GetSection("urls"));

            var startupSecrets = new StartupSecretsConfig();
            Configuration.GetSection("SdraDbSection").Bind(startupSecrets);
            Configuration.GetSection("LoggingDbSection").Bind(startupSecrets);

            services.AddOptions<SecretsConfig>().Bind(Configuration.GetSection("SdraWebservice"));

            LoggingHelper.SetupLogging(isLocalDb, startupLoggingConfig, startupSecrets);

            var connection = Common.Helpers.DatabasesHelpers.BuildOracleConnectionString(startupSecrets.DataSource,
                startupSecrets.UserId, startupSecrets.Password);

            services.AddDbContext<SdraDbContext>((serviceProvider, options) =>
                options.UseOracle(connection));

            services.AddControllers().AddJsonOptions(o =>
                {
                    //o.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    //o.SerializerSettings.Converters.Add(new StringEnumConverter
                    //{
                    //    CamelCaseText = true
                    //});
                });

            services
                .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("1.0.0", new OpenApiInfo
                    {
                        Version = "1.0.0",
                        Title = "SDRA API",
                        Description = "SDRA API (ASP.NET Core 3.1)",
                    });
                    c.IncludeXmlComments(Path.Combine(Directory.GetCurrentDirectory(), "DataServices.xml"));

                    // TODO fails at runtime
                    // Sets the basePath property in the Swagger document generated
                    // c.DocumentFilter<BasePathFilter>("/DataServices/v1/");

                    // TODO fails at runtime
                    // Include DataAnnotation attributes on Controller Action parameters as Swagger validation rules (e.g required, pattern, ..)
                    // Use [ValidateModelState] on Actions to actually validate it in C# as well!
                    //c.OperationFilter<GeneratePathParamsValidationFilter>();
                });

            services.AddScoped<IAssessmentWebServiceSoapClientAdapter, AssessmentWebServiceSoapClientAdapter>();
            services.AddScoped<IDataAccessWebServiceSoapClientAdapter, DataAccessWebServiceSoapClientAdapter>();

            services.AddHealthChecks();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app
                .UseSerilogRequestLogging(
                    options =>
                        options.GetLevel = (ctx, d, ex) =>
                        {
                            if (ex == null && ctx.Response.StatusCode <= 499)
                            {
                                if (ctx.Request.RouteValues.Any()) //Request is a page
                                {
                                    return LogEventLevel.Information;
                                }

                                return LogEventLevel.Verbose;
                            }

                            return LogEventLevel.Error;
                        }
                )
                .UseHttpsRedirection()
                .UseDefaultFiles()
                .UseStaticFiles()
                .UseSwagger()
                .UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger-original.json", "SDRA API Original"); })

                .UseRouting()

                // .UseCors()
                // .UseAuthentication()
                // .UseAuthorization()

                .UseEndpoints(endpoints =>
                {
                    endpoints.MapHealthChecks("/health");
                    endpoints.MapControllers();
                });



            if (ConfigHelpers.IsLocalDevelopment || ConfigHelpers.IsAzureDevelopment)
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                //TODO: Enable production exception handling (https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
                // app.UseExceptionHandler("/Home/Error");
            }
        }
    }
}
