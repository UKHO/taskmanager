using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;

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
                transport.ConnectionString("TBC");
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
