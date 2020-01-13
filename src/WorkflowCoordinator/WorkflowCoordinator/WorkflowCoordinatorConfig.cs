using System;
using System.Data.SqlClient;
using Common.Messages.Commands;
using Common.Messages.Events;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Transport.SQLServer;
using WorkflowCoordinator.Config;

namespace WorkflowCoordinator
{
    public class WorkflowCoordinatorConfig : EndpointConfiguration
    {
        public WorkflowCoordinatorConfig(NsbConfig nsbConfig,
            NsbSecretsConfig nsbSecretsConfig) : base(nsbConfig.WorkflowCoordinatorName)
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
                            if (!nsbConfig.IsLocalDevelopment) con.AccessToken = nsbSecretsConfig.AzureAccessToken;
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

            routing.RouteToEndpoint(
                messageType: typeof(InitiateSourceDocumentRetrievalEvent),
                destination: nsbConfig.SourceDocumentCoordinatorName);

            routing.RouteToEndpoint(
                messageType: typeof(GetBackwardDocumentLinksCommand),
                destination: nsbConfig.SourceDocumentCoordinatorName);

            routing.RouteToEndpoint(
                messageType: typeof(GetForwardDocumentLinksCommand),
                destination: nsbConfig.SourceDocumentCoordinatorName);

            routing.RouteToEndpoint(
                messageType: typeof(GetSepDocumentLinksCommand),
                destination: nsbConfig.SourceDocumentCoordinatorName);

            routing.RegisterPublisher(
                assembly: typeof(StartWorkflowInstanceEvent).Assembly,
                publisherEndpoint: nsbConfig.EventServiceName);

            // Persistence

            var persistence = this.UsePersistence<SqlPersistence>();
            persistence.SqlDialect<SqlDialect.MsSqlServer>();
            persistence.ConnectionBuilder(connectionBuilder: () =>
            {
                var con = new SqlConnection();
                try
                {
                    con.ConnectionString = nsbSecretsConfig.NsbDbConnectionString;
                    if (!nsbConfig.IsLocalDevelopment) con.AccessToken = nsbSecretsConfig.AzureAccessToken;
                    return con;
                }
                catch
                {
                    con.Dispose();
                    throw;
                }
            });
            persistence.SubscriptionSettings().CacheFor(TimeSpan.FromMinutes(1));

            // Additional config

            this.Conventions()
                .DefiningCommandsAs(type => type.Namespace == "Common.Messages.Commands")
                .DefiningEventsAs(type => type.Namespace == "Common.Messages.Events")
                .DefiningMessagesAs(type => type.Namespace == "Common.Messages")
                .DefiningMessagesAs(type => type.Namespace == "WorkflowCoordinator.Messages");

            this.AssemblyScanner().ScanAssembliesInNestedDirectories = true;
            this.EnableInstallers();
            this.UseSerialization<NewtonsoftSerializer>();

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
    }
}