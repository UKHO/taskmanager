using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Options;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;
using NServiceBus.Transport.SQLServer;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCoordinator.Config;

namespace WorkflowCoordinator
{
    public class NServiceBusJobHost : IJobHost
    {
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IOptions<SecretsConfig> _secretsConfig;
        static readonly ILog Log = LogManager.GetLogger<NServiceBusJobHost>();

        private IEndpointInstance _endpoint;

        public NServiceBusJobHost(IOptions<GeneralConfig> generalConfig, IOptions<SecretsConfig> secretsConfig)
        {
            _generalConfig = generalConfig;
            _secretsConfig = secretsConfig;
        }

        [FunctionName("StartAsync")]
        [NoAutomaticTrigger]
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var endpointConfiguration = new EndpointConfiguration(_generalConfig.Value.NsbEndpointName);

                var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
                transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
                transport.UseCustomSqlConnectionFactory(
                    sqlConnectionFactory: async () =>
                    {
                        var con = new SqlConnection();
                        try
                        {
                            var azureServiceTokenProvider = new AzureServiceTokenProvider();
                            var azureDbTokenUrl = _generalConfig.Value.ConnectionStrings.AzureDbTokenUrl;
                            var token = await azureServiceTokenProvider.GetAccessTokenAsync(azureDbTokenUrl.ToString());

                            var builder = new SqlConnectionStringBuilder();
                            builder["Data Source"] = "";
                            builder["Initial Catalog"] = "";
                            builder["Connect Timeout"] = 30;
                            // TODO - do we need all this?
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

                _endpoint = await Endpoint.Start(endpointConfiguration);
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
                await _endpoint?.Stop();
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
                Log.Fatal(message, exception);
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
