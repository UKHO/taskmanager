using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Common.Messages.Events;
using Microsoft.Azure.Services.AppAuthentication;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Transport.SQLServer;
using SourceDocumentCoordinator.Config;

namespace SourceDocumentCoordinator
{
    public class SourceDocumentCoordinatorConfig : EndpointConfiguration
    {
        public SourceDocumentCoordinatorConfig(NsbConfig nsbConfig,
            NsbSecretsConfig nsbSecretsConfig, AzureServiceTokenProvider azureServiceTokenProvider = null) : base(nsbConfig.SourceDocumentCoordinatorName)
        {
            // Transport

            var transport = this.UseTransport<SqlServerTransport>()
                .Transactions(TransportTransactionMode.SendsAtomicWithReceive).UseCustomSqlConnectionFactory(
                    async () =>
                    {
                        var con = new SqlConnection();
                        try
                        {
                            con.ConnectionString = nsbSecretsConfig.NsbDbConnectionString;
                            if (!nsbConfig.IsLocalDevelopment && azureServiceTokenProvider != null)
                            {
                                con.AccessToken = await azureServiceTokenProvider.GetAccessTokenAsync(nsbConfig.AzureDbTokenUrl.ToString());
                            }
                            await con.OpenAsync().ConfigureAwait(false);
                            return con;
                        }
                        catch
                        {
                            con.Dispose();
                            throw;
                        }
                    });

            // Routing

            var routing = transport.Routing();

            routing.RegisterPublisher(
                assembly: typeof(InitiateSourceDocumentRetrievalEvent).Assembly,
                publisherEndpoint: nsbConfig.EventServiceName);

            routing.RegisterPublisher(
                assembly: typeof(InitiateSourceDocumentRetrievalEvent).Assembly,
                publisherEndpoint: nsbConfig.WorkflowCoordinatorName);

            // Persistence

            var persistence = this.UsePersistence<SqlPersistence>();
            persistence.SqlDialect<SqlDialect.MsSqlServer>();
            persistence.ConnectionBuilder(connectionBuilder: () =>
            {
                var con = new SqlConnection();
                try
                {
                    con.ConnectionString = nsbSecretsConfig.NsbDbConnectionString;
                    if (!nsbConfig.IsLocalDevelopment) con.AccessToken = azureServiceTokenProvider.GetAccessTokenAsync(nsbConfig.AzureDbTokenUrl.ToString()).Result;
                    return con;
                }
                catch
                {
                    con.Dispose();
                    throw;
                }
            });
            persistence.SubscriptionSettings().CacheFor(TimeSpan.FromMinutes(1));

            this.AuditProcessedMessagesTo("audit", TimeSpan.FromMinutes(10));
            this.SendFailedMessagesTo("error");

            // Additional config

            this.Conventions()
                .DefiningCommandsAs(type => type.Namespace == "Common.Messages.Commands")
                .DefiningEventsAs(type => type.Namespace == "Common.Messages.Events")
                .DefiningMessagesAs(type => type.Namespace == "Common.Messages")
                .DefiningMessagesAs(type => type.Namespace == "SourceDocumentCoordinator.Messages");

            this.AssemblyScanner().ScanAssembliesInNestedDirectories = true;
            this.EnableInstallers();
            this.UseSerialization<NewtonsoftSerializer>();
            this.DefineCriticalErrorAction(OnCriticalError);

            // Additional config for local development

            if (nsbConfig.IsLocalDevelopment)
            {
                var recoverability = this.Recoverability();
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
        }

        #region WebJobHost_CriticalError
        // Need to collect Application Events in Azure to see this
        static async Task OnCriticalError(ICriticalErrorContext context)
        {
            var fatalMessage =
                $"The following critical error was encountered:{Environment.NewLine}{context.Error}{Environment.NewLine}Process is shutting down. StackTrace: {Environment.NewLine}{context.Exception.StackTrace}";
            EventLog.WriteEntry(".NET Runtime", fatalMessage, EventLogEntryType.Error);

            try
            {
                await context.Stop().ConfigureAwait(false);
            }
            finally
            {
                Environment.FailFast(fatalMessage, context.Exception);
            }
        }
        #endregion
    }
}
