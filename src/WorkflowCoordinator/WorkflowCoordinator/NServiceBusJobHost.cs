using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Persistence.Sql;
using NServiceBus.Transport.SQLServer;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.Messages;

namespace WorkflowCoordinator
{
    public class NServiceBusJobHost : IJobHost
    {
        private readonly IOptions<SecretsConfig> _secretsConfig;
        private readonly IOptions<GeneralConfig> _generalConfig;
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
            EndpointConfiguration endpointConfig)
        {
            _generalConfig = generalConfig;
            _secretsConfig = secretsConfig;
            _hostingEnvironment = hostingEnvironment;
            _endpointConfig = endpointConfig;

            _isLocalDebugging = _hostingEnvironment.IsDevelopment() && Debugger.IsAttached;
            _localDbServer = @"(localdb)\MSSQLLocalDB";

            if (_isLocalDebugging)
            {
                _connectionString = BuildSqlConnectionString(_localDbServer, _secretsConfig.Value.NsbInitialCatalog);
                ReCreateLocalDb(_secretsConfig.Value.NsbInitialCatalog, BuildSqlConnectionString(_localDbServer));
            }
            else
            {
                _connectionString = BuildSqlConnectionString(_secretsConfig.Value.NsbDataSource, _secretsConfig.Value.NsbInitialCatalog);

                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var azureDbTokenUrl = _generalConfig.Value.AzureDbTokenUrl;
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

        private string BuildSqlConnectionString(string dataSource, string initialCatalog = "") =>
            new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                InitialCatalog = initialCatalog,
                IntegratedSecurity = _isLocalDebugging,
                Encrypt = _isLocalDebugging ? false : true,
                ConnectTimeout = 20
            }.ToString();

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

                // Works but is not testable via unit tests
                //transport.Routing().RouteToEndpoint(
                //    messageType: typeof(InitiateSourceDocumentRetrievalCommand),
                //    destination: "SourceDocumentCoordinator");

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
                    .DefiningMessagesAs(type => type.Namespace == "WorkflowCoordinator.Messages");
                ;
                endpointConfiguration.AssemblyScanner().ScanAssembliesInNestedDirectories = true;
                endpointConfiguration.EnableInstallers();
                _endpoint = await Endpoint.Start(endpointConfiguration);


                // TODO concerns over restarting endpoint
                // Send the initial delayed message for polling
                var options = new SendOptions();
                options.DelayDeliveryWith(TimeSpan.FromSeconds(5));
                options.RouteToThisEndpoint();
                await _endpoint.Send(new OpenAssessmentPollingMessage(), options).ConfigureAwait(false);
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
