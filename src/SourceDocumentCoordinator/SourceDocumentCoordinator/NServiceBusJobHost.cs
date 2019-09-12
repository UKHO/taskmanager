using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Commands;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Persistence.Sql;
using NServiceBus.Transport.SQLServer;
using SourceDocumentCoordinator.Config;

namespace SourceDocumentCoordinator
{
    public class NServiceBusJobHost : IJobHost
    {
        private readonly IOptions<SecretsConfig> _secretsConfig;
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly EndpointConfiguration _endpointConfig;
        private readonly string _localDbServer;

        private readonly bool _isLocalDebugging;
        private readonly string _connectionString;
        private readonly string _azureAccessToken;

        static readonly ILog Log = LogManager.GetLogger<NServiceBusJobHost>();

        private IEndpointInstance _endpoint;

        public NServiceBusJobHost(IOptionsSnapshot<GeneralConfig> generalConfig,
            IOptionsSnapshot<SecretsConfig> secretsConfig,
            IHostingEnvironment hostingEnvironment,
            EndpointConfiguration endpointConfig, IOptions<UriConfig> uriConfig)
        {
            _generalConfig = generalConfig;
            _secretsConfig = secretsConfig;
            _hostingEnvironment = hostingEnvironment;
            _endpointConfig = endpointConfig;
            _uriConfig = uriConfig;

            _isLocalDebugging = ConfigHelpers.IsLocalDevelopment;
            _localDbServer = _generalConfig.Value.LocalDbServer;

            if (_isLocalDebugging)
            {
                _connectionString = DatabasesHelpers.BuildSqlConnectionString(_isLocalDebugging, _localDbServer, _secretsConfig.Value.NsbInitialCatalog);
                ReCreateLocalDb(_secretsConfig.Value.NsbInitialCatalog, DatabasesHelpers.BuildSqlConnectionString(_isLocalDebugging, _localDbServer));
            }
            else
            {
                _connectionString = DatabasesHelpers.BuildSqlConnectionString(_isLocalDebugging, _secretsConfig.Value.NsbDataSource, _secretsConfig.Value.NsbInitialCatalog);

                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var azureDbTokenUrl = _uriConfig.Value.AzureDbTokenUrl;
                _azureAccessToken = azureServiceTokenProvider.GetAccessTokenAsync(azureDbTokenUrl.ToString()).Result;
            }
        }

        private void ReCreateLocalDb(string dbName, string connectionString)
        {
            var connectionStringObject = new SqlConnectionStringBuilder(connectionString);
            if (!_isLocalDebugging || !connectionStringObject.DataSource.Equals(_localDbServer))
            {
                throw new InvalidOperationException($@"{nameof(ReCreateLocalDb)} should only be called when executing in local development environment.");
            }

            var sanitisedDbName = dbName.Replace("'", "''");

            var commandText = "USE master " +
                        $"IF EXISTS(select * from sys.databases where name='{sanitisedDbName}') " +
                        "BEGIN " +
                       $"ALTER DATABASE [{sanitisedDbName}] " +
                        "SET SINGLE_USER " +
                        "WITH ROLLBACK IMMEDIATE; " +
                        $"DROP DATABASE [{sanitisedDbName}] " +
                        $"CREATE DATABASE [{sanitisedDbName}] " +
                        "END " +
                        "ELSE " +
                        "BEGIN " +
                        $"CREATE DATABASE [{sanitisedDbName}] " +
                        "END";

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand(commandText, connection);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                finally
                {
                    connection.Close();
                }

            }
        }

        [FunctionName("StartAsync")]
        [NoAutomaticTrigger]
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var endpointConfiguration = _endpointConfig;
                var transport = endpointConfiguration.UseTransport<SqlServerTransport>()
                    .Transactions(TransportTransactionMode.SendsAtomicWithReceive).UseCustomSqlConnectionFactory(
                        async () =>
                        {
                            var con = new SqlConnection();
                            try
                            {
                                con.ConnectionString = _connectionString;
                                if (!_isLocalDebugging) con.AccessToken = _azureAccessToken;
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
                        con.ConnectionString = _connectionString;
                        if (!_isLocalDebugging) con.AccessToken = _azureAccessToken;
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
                    .DefiningMessagesAs(type => type.Namespace == "Common.Messages")
                    .DefiningMessagesAs(type => type.Namespace == "SourceDocumentCoordinator.Messages");
                ;
                endpointConfiguration.AssemblyScanner().ScanAssembliesInNestedDirectories = true;
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
