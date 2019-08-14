using System;
using System.Threading.Tasks;
using Common.Messages.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NServiceBus;
using NServiceBus.Logging;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowDatabase.EF;

namespace WorkflowCoordinator.Sagas
{
    public class AssessmentPollingSaga : Saga<AssessmentPollingSagaData>,
            IAmStartedByMessages<StartAssessmentPollingCommand>,
            IHandleTimeouts<ExecuteAssessmentPollingTask>
    {
        private readonly IOptionsSnapshot<GeneralConfig> _generalConfig;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly WorkflowDbContext _dbContext;

        ILog log = LogManager.GetLogger<AssessmentPollingSaga>();

        public AssessmentPollingSaga(IOptionsSnapshot<GeneralConfig> generalConfig,
            IDataServiceApiClient dataServiceApiClient, WorkflowDbContext dbContext)
        {
            _generalConfig = generalConfig;
            _dataServiceApiClient = dataServiceApiClient;
            _dbContext = dbContext;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AssessmentPollingSagaData> mapper)
        {
            mapper.ConfigureMapping<StartAssessmentPollingCommand>(message => message.CorrelationId)
                  .ToSaga(sagaData => sagaData.CorrelationId);
        }

        public async Task Handle(StartAssessmentPollingCommand message, IMessageHandlerContext context)
        {
            log.Debug($"Handling {nameof(StartAssessmentPollingCommand)}");

            Data.CorrelationId = message.CorrelationId;
            if (!Data.IsTaskAlreadyScheduled)
            {
                await RequestTimeout<ExecuteAssessmentPollingTask>(context,
                        TimeSpan.FromSeconds(_generalConfig.Value.WorkflowCoordinatorAssessmentPollingIntervalSeconds))
                    .ConfigureAwait(false);
                Data.IsTaskAlreadyScheduled = true;
            }
        }

        public async Task Timeout(ExecuteAssessmentPollingTask state, IMessageHandlerContext context)
        {
            log.Debug($"Handling timeout {nameof(ExecuteAssessmentPollingTask)}");

            var assessments = await _dataServiceApiClient.GetAssessments(_generalConfig.Value.CallerCode);

            foreach (var assessment in assessments)
            {
                var assessmentRecord = await
                    _dbContext.AssessmentData.SingleOrDefaultAsync(a => a.RsdraNumber == assessment.RsdraNumber);

                if (assessmentRecord == null)
                {
                    // Put bits to get rest of SDRA data and add row to our Db here when we hit those stories

                    var startDbAssessmentCommand = new StartDbAssessmentCommand()
                    {
                        CorrelationId = Guid.NewGuid()
                    };

                    await context.Send(startDbAssessmentCommand).ConfigureAwait(false);

                    var initiateRetrievalCommand = new InitiateSourceDocumentRetrievalCommand()
                    {
                        SourceDocumentId = assessment.SdocId,
                        CorrelationId = Guid.NewGuid()
                    };

                    await context.Send(initiateRetrievalCommand).ConfigureAwait(false);
                }
            }

            await RequestTimeout<ExecuteAssessmentPollingTask>(context,
                    TimeSpan.FromSeconds(_generalConfig.Value.WorkflowCoordinatorAssessmentPollingIntervalSeconds))
                .ConfigureAwait(false);
        }
    }
}
