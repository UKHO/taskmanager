using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
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
