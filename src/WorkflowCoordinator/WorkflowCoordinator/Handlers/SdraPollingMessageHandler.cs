using NServiceBus;
using NServiceBus.Logging;
using System;
using System.Threading.Tasks;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;

namespace WorkflowCoordinator.Handlers
{
    public class SdraPollingMessageHandler : IHandleMessages<SdraPollingMessage>
    {
        private readonly IDataServiceApiClient _dataServiceApiClient;
        ILog log = LogManager.GetLogger<SdraPollingMessage>();

        public SdraPollingMessageHandler(IDataServiceApiClient dataServiceApiClient)
        {
            _dataServiceApiClient = dataServiceApiClient;
        }

        public async Task Handle(SdraPollingMessage message, IMessageHandlerContext context)
        {
            // TODO does HDB caller code need to be in config or is it never changing?
            var assessments = await _dataServiceApiClient.GetAssessments("HDB");

            log.Debug($"[Defer Message Delivery] for {nameof(SdraPollingMessage)}");

            var options = new SendOptions();
            options.DelayDeliveryWith(TimeSpan.FromSeconds(5));
            options.RouteToThisEndpoint();

            await context.Send(new SdraPollingMessage(), options)
                 .ConfigureAwait(false);
        }
    }
}
