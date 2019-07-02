using System;

using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;
using NServiceBus.Transport.SQLServer;

namespace WorkflowCoordinator
{
    public class TestHost : IJobHost
    {
        static readonly ILog log = LogManager.GetLogger<TestHost>();

        IEndpointInstance endpoint;

        public string EndpointName => "UKHO.TaskManager.WorkflowCoordinator";


        // TODO check which attributes we need
        [FunctionName("StartAsync")]
        [NoAutomaticTrigger]

        public async Task StartAsync(CancellationToken cancellationToken)
        { 
            try
            {
                var endpointConfiguration = new EndpointConfiguration(EndpointName);
                #region TransportConfiguration

                var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
                transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
                transport.UseCustomSqlConnectionFactory(
                    sqlConnectionFactory: async () =>
                    {
                        //var connection = new SqlConnection("Data Source=tcp:taskmanager-dev-sqlserver.database.windows.net,1433;Initial Catalog=NServiceBus;");
                        var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString);
                        try
                        {
                            var azureServiceTokenProvider = new AzureServiceTokenProvider();
                            var connectionAccessToken = azureServiceTokenProvider.GetAccessTokenAsync("https://database.windows.net/").Result;
                            connection.AccessToken = connectionAccessToken;
                            await connection.OpenAsync()
                                .ConfigureAwait(false);


                            
                            // perform custom operations

                            return connection;
                        }
                        catch
                        {
                            connection.Dispose();
                            throw;
                        }
                    });
                //transport.UseCustomSqlConnectionFactory() ??
                //transport.UseConventionalRoutingTopology();

                #endregion
                endpointConfiguration.DisableFeature<TimeoutManager>();
                endpointConfiguration.DisableFeature<MessageDrivenSubscriptions>();

                endpointConfiguration.EnableInstallers();

                endpoint = await Endpoint.Start(endpointConfiguration);
            }
            catch (Exception ex)
            {
                FailFast("Failed to start.", ex);
            }
        }

        public async Task StopAsync()
        {
            try
            {
                await endpoint?.Stop();
            }
            catch (Exception ex)
            {
                FailFast("Failed to stop correctly.", ex);
            }
        }

        async Task OnCriticalError(ICriticalErrorContext context)
        {
            try
            {
                await context.Stop();
            }
            finally
            {
                FailFast($"Critical error, shutting down: {context.Error}", context.Exception);
            }
        }

        void FailFast(string message, Exception exception)
        {
            try
            {
                log.Fatal(message, exception);
            }
            finally
            {
                Environment.FailFast(message, exception);
            }
        }


        public Task CallAsync(string name, IDictionary<string, object> arguments = null, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }
    }
}
