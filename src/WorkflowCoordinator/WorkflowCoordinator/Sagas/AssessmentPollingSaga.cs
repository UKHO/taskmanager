using System;
using System.Threading.Tasks;
using Common.Messages.Events;
using DataServices.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NServiceBus;
using Serilog.Context;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowDatabase.EF;
using Task = System.Threading.Tasks.Task;

namespace WorkflowCoordinator.Sagas
{
    public class AssessmentPollingSaga : Saga<AssessmentPollingSagaData>,
            IAmStartedByMessages<StartAssessmentPollingCommand>,
            IHandleTimeouts<ExecuteAssessmentPollingTask>
    {
        private readonly IOptionsSnapshot<GeneralConfig> _generalConfig;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly WorkflowDbContext _dbContext;
        private readonly ILogger<AssessmentPollingSaga> _logger;

        public AssessmentPollingSaga(IOptionsSnapshot<GeneralConfig> generalConfig,
            IDataServiceApiClient dataServiceApiClient, WorkflowDbContext dbContext,
            ILogger<AssessmentPollingSaga> logger)
        {
            _generalConfig = generalConfig;
            _dataServiceApiClient = dataServiceApiClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AssessmentPollingSagaData> mapper)
        {
            mapper.ConfigureMapping<StartAssessmentPollingCommand>(message => message.CorrelationId)
                  .ToSaga(sagaData => sagaData.CorrelationId);
        }

        public async Task Handle(StartAssessmentPollingCommand message, IMessageHandlerContext context)
        {
            _logger.LogInformation($"Handling {nameof(StartAssessmentPollingCommand)}");

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
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("CorrelationId", "");
            LogContext.PushProperty("EventName", nameof(ExecuteAssessmentPollingTask));
            LogContext.PushProperty("ProcessId", 0);


            _logger.LogInformation($"Handling timeout {nameof(ExecuteAssessmentPollingTask)}");

            var assessment = await GetAssessmentNotInWorkflowDatabase();

            if (assessment != null)
            {
                var correlationId = Guid.NewGuid();

                // Start new DB Assessment K2 workflow instance
                // Then Get SDRA data from SDRA-DB and store it in WorkflowDatabase
                var startDbAssessmentCommand = new StartDbAssessmentCommand()
                {
                    CorrelationId = correlationId,
                    SourceDocumentId = assessment.Id
                };

                await context.SendLocal(startDbAssessmentCommand).ConfigureAwait(false);
            }

            await RequestTimeout<ExecuteAssessmentPollingTask>(context,
                    TimeSpan.FromSeconds(_generalConfig.Value.WorkflowCoordinatorAssessmentPollingIntervalSeconds))
                .ConfigureAwait(false);
        }

        private async Task<DocumentObject> GetAssessmentNotInWorkflowDatabase()
        {
            var assessments = await _dataServiceApiClient.GetAssessments(_generalConfig.Value.CallerCode);

            // Get first Open Assessment that does not exists in WorkflowDatabase
            foreach (var assessment in assessments)
            {
                var isExists = await
                    _dbContext.AssessmentData.AnyAsync(a => a.PrimarySdocId == assessment.Id);

                if (!isExists) return assessment;
            }

            return null;
        }
    }
}
