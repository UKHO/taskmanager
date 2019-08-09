using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowDatabase.EF;

namespace WorkflowCoordinator.Handlers
{
    public class SdraPollingMessageHandler : IHandleMessages<SdraPollingMessage>
    {
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly WorkflowDbContext _dbContext;
        ILog log = LogManager.GetLogger<SdraPollingMessage>();

        public SdraPollingMessageHandler(IDataServiceApiClient dataServiceApiClient, WorkflowDbContext dbContext)
        {
            _dataServiceApiClient = dataServiceApiClient;
            _dbContext = dbContext;
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
