using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;
using NServiceBus.Transport.SQLServer;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

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

                var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
                transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
                transport.UseCustomSqlConnectionFactory(
                    sqlConnectionFactory: async () =>
                    {
                        var con = new SqlConnection();
                        try
                        {
                            var azureServiceTokenProvider = new AzureServiceTokenProvider();
                            var token = await azureServiceTokenProvider.GetAccessTokenAsync(
                                "https://database.windows.net/");

                            var builder = new SqlConnectionStringBuilder();
                            builder["Data Source"] = "";
                            builder["Initial Catalog"] = "";
                            builder["Connect Timeout"] = 30;
                            builder["Persist Security Info"] = false;
                            builder["TrustServerCertificate"] = false;
                            builder["Encrypt"] = true;
                            builder["MultipleActiveResultSets"] = false;

                            con.ConnectionString = builder.ToString();
                            con.AccessToken = token;
                            await con.OpenAsync().ConfigureAwait(false);

                            return con;
                        }
                        catch
                        {
                            con.Dispose();
                            throw;
                        }
                    });

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
