using System;
using System.Data.SqlClient;
using System.Linq;
using Common.Helpers;
using EventService.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using NServiceBus;
using NServiceBus.Extensions.DependencyInjection;
using NServiceBus.Persistence.Sql;
using NServiceBus.Transport.SQLServer;
using Serilog;
using Serilog.Events;

namespace EventService
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
            var isLocalDb = ConfigHelpers.IsLocalDevelopment;
            var startupLoggingConfig = new StartupLoggingConfig();
            Configuration.GetSection("logging").Bind(startupLoggingConfig);

            var startupSecretsConfig = new StartupSecretsConfig();
            Configuration.GetSection("NsbDbSection").Bind(startupSecretsConfig);
            Configuration.GetSection("LoggingDbSection").Bind(startupSecretsConfig);

            LoggingHelper.SetupLogging(isLocalDb, startupLoggingConfig, startupSecretsConfig);

            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });
            services.AddMvc();
            services.AddHealthChecks();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("1.0.0", new OpenApiInfo()
                {
                    Version = "1.0.0",
                    Title = "",
                    Description = ""
                });
            });

            var isLocalDebugging = ConfigHelpers.IsLocalDevelopment;
            var startupConfig = new StartupConfig();
            Configuration.GetSection("urls").Bind(startupConfig);
            Configuration.GetSection("databases").Bind(startupConfig);
            Configuration.GetSection("nsb").Bind(startupConfig);

            string connectionString = null;

            var endpointConfiguration = new EndpointConfiguration(startupConfig.EventServiceName);

            if (isLocalDebugging)
            {
                connectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDebugging,
                    startupConfig.LocalDbServer,
                    startupSecretsConfig.NsbInitialCatalog);
                DatabasesHelpers.ReCreateLocalDb(startupConfig.LocalDbServer,
                    startupSecretsConfig.NsbInitialCatalog,
                    DatabasesHelpers.BuildSqlConnectionString(isLocalDebugging, startupConfig.LocalDbServer),
                    isLocalDebugging);
                var recoverability = endpointConfiguration.Recoverability();
                recoverability.Immediate(
                    immediate =>
                    {
                        immediate.NumberOfRetries(0);
                    });

                recoverability.Delayed(
                    delayed =>
                    {
                        delayed.NumberOfRetries(0);
                    });
            }
            else
            {
                connectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDebugging, startupSecretsConfig.NsbDataSource, startupSecretsConfig.NsbInitialCatalog);
            }

            // Implicit singleton to reduce load on GC (transport SQL connection factory delegate is called continually).
            // Not required for its internal cache, which is static.
            var azureServiceTokenProvider = new AzureServiceTokenProvider();

            var transport = endpointConfiguration.UseTransport<SqlServerTransport>()
                .Transactions(TransportTransactionMode.SendsAtomicWithReceive).UseCustomSqlConnectionFactory(
                    async () =>
                    {
                        var con = new SqlConnection();
                        try
                        {
                            con.ConnectionString = connectionString;
                            if (!isLocalDebugging) con.AccessToken = await azureServiceTokenProvider.GetAccessTokenAsync(startupConfig.AzureDbTokenUrl.ToString()).ConfigureAwait(false);
                            await con.OpenAsync().ConfigureAwait(false);
                            return con;
                        }
                        catch
                        {
                            con.Dispose();
                            throw;
                        }
                    });

            var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
            persistence.SqlDialect<SqlDialect.MsSqlServer>();
            persistence.ConnectionBuilder(connectionBuilder: () =>
            {
                var con = new SqlConnection();
                try
                {
                    con.ConnectionString = connectionString;
                    if (!isLocalDebugging) con.AccessToken = azureServiceTokenProvider.GetAccessTokenAsync(startupConfig.AzureDbTokenUrl.ToString()).Result; ;
                    return con;
                }
                catch
                {
                    con.Dispose();
                    throw;
                }
            });
            persistence.SubscriptionSettings().CacheFor(TimeSpan.FromMinutes(1));

            endpointConfiguration.Conventions()
                .DefiningCommandsAs(type => type.Namespace == "Common.Messages.Commands")
                .DefiningEventsAs(type => type.Namespace == "Common.Messages.Events")
                .DefiningMessagesAs(type => type.Namespace == "Common.Messages");

            endpointConfiguration.AssemblyScanner().ScanAssembliesInNestedDirectories = true;
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.UseSerialization<NewtonsoftSerializer>();

            services.AddNServiceBus(endpointConfiguration);
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
                            if (ctx.Request.RouteValues.Any()) //Request is a page
                            {
                                return LogEventLevel.Information;
                            }

                            return LogEventLevel.Verbose;
                        }

                        return LogEventLevel.Error;
                    }
            );

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseStaticFiles();

            app.UseAuthorization();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger-original.json", "Event Service API Original");
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }
    }
}
