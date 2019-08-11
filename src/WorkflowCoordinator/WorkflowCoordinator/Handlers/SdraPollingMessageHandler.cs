using System;
using System.Threading.Tasks;
using Common.Messages;
using Microsoft.EntityFrameworkCore;
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

            foreach (var assessment in assessments)
            {
                var assessmentRecord = await
                    _dbContext.AssessmentData.SingleOrDefaultAsync(a => a.RsdraNumber == assessment.RsdraNumber);

                if (assessmentRecord == null)
                {
                    // TODO Put bits to get rest of SDRA data and add row to our Db here

                    var startDbAssessmentCommand = new StartDbAssessmentCommand()
                    {
                        CorrelationId = Guid.NewGuid()
                    };

                    var startDbAssessmentCommandOptions = new SendOptions();
                    startDbAssessmentCommandOptions.RouteToThisEndpoint();
                    await context.Send(startDbAssessmentCommand, startDbAssessmentCommandOptions).ConfigureAwait(false);

                    var initiateRetrievalCommand = new InitiateSourceDocumentRetrievalCommand()
                    {
                        SourceDocumentId = assessment.SdocId,
                        CorrelationId = Guid.NewGuid()
                    };

                    // TODO prefer routing centralised
                    await context.Send("SourceDocumentCoordinator", initiateRetrievalCommand).ConfigureAwait(false);
                }
            }

            var sdraPollingMessageOptions = new SendOptions();
            sdraPollingMessageOptions.DelayDeliveryWith(TimeSpan.FromSeconds(5));
            sdraPollingMessageOptions.RouteToThisEndpoint();
            await context.Send(new SdraPollingMessage(), sdraPollingMessageOptions)
                 .ConfigureAwait(false);
        }
    }
}
