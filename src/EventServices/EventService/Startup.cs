using System;
using System.Data.SqlClient;
using Common.Helpers;
using EventService.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NServiceBus;
using NServiceBus.Extensions.DependencyInjection;
using NServiceBus.Persistence.Sql;
using NServiceBus.Transport.SQLServer;

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
            services.AddControllers();
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

            string azureAccessToken = null;
            var isLocalDebugging = ConfigHelpers.IsLocalDevelopment;
            var startupConfig = new StartupConfig();
            Configuration.GetSection("urls").Bind(startupConfig);
            Configuration.GetSection("databases").Bind(startupConfig);
            Configuration.GetSection("nsb").Bind(startupConfig);

            var startupSecretsConfig = new StartupSecretsConfig();
            Configuration.GetSection("NsbDbSection").Bind(startupSecretsConfig);

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

                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var azureDbTokenUrl = startupConfig.AzureDbTokenUrl;
                azureAccessToken = azureServiceTokenProvider.GetAccessTokenAsync(azureDbTokenUrl.ToString()).Result;
            }

            var transport = endpointConfiguration.UseTransport<SqlServerTransport>()
                .Transactions(TransportTransactionMode.SendsAtomicWithReceive).UseCustomSqlConnectionFactory(
                    async () =>
                    {
                        var con = new SqlConnection();
                        try
                        {
                            con.ConnectionString = connectionString;
                            if (!isLocalDebugging) con.AccessToken = azureAccessToken;
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
                    if (!isLocalDebugging) con.AccessToken = azureAccessToken;
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

            services.AddNServiceBus(endpointConfiguration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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
                c.SwaggerEndpoint("/swagger-original.json", "SDRA API Original");
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }
    }
}
