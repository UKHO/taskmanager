using System;
using System.Threading.Tasks;
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
using WorkflowDatabase.EF.Models;
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
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("MessageCorrelationId", "");
            LogContext.PushProperty("EventName", nameof(StartAssessmentPollingCommand));
            LogContext.PushProperty("ProcessId", 0);

            _logger.LogInformation("Handling {EventName}");

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
            LogContext.PushProperty("MessageCorrelationId", "");
            LogContext.PushProperty("EventName", nameof(ExecuteAssessmentPollingTask));
            LogContext.PushProperty("ProcessId", 0);
            
            _logger.LogInformation("Handling timeout {EventName}");

            var assessment = await GetAssessmentNotInWorkflowDatabase();

            if (assessment != null)
            {
                LogContext.PushProperty("PrimarySdocId", assessment.Id);
                _logger.LogInformation("PrimarySdocId {PrimarySdocId} belongs to open assessment not in our system");

                // Open assessment sdoc id not yet in our system
                await AddSdocIdToQueue(assessment.Id);

                // Fire StartDbAssessmentCommand to create K2 workflow instance,
                // get assessment data from SDRA, and update our DB
                await FireStartDbAssessmentCommand(context, assessment.Id);
            }

            await RequestTimeout<ExecuteAssessmentPollingTask>(context,
                    TimeSpan.FromSeconds(_generalConfig.Value.WorkflowCoordinatorAssessmentPollingIntervalSeconds))
                .ConfigureAwait(false);
        }

        private async Task FireStartDbAssessmentCommand(IMessageHandlerContext context, int primarySdocId)
        {
            var correlationId = Guid.NewGuid();

            LogContext.PushProperty("MessageCorrelationId", correlationId);

            _logger.LogInformation(
                "Firing StartDbAssessmentCommand for PrimarySdocId {PrimarySdocId} with MessageCorrelationId {MessageCorrelationId}");

            var startDbAssessmentCommand = new StartDbAssessmentCommand()
            {
                CorrelationId = correlationId,
                SourceDocumentId = primarySdocId
            };

            await context.SendLocal(startDbAssessmentCommand).ConfigureAwait(false);
        }

        private async Task<DocumentObject> GetAssessmentNotInWorkflowDatabase()
        {
            var assessments = await _dataServiceApiClient.GetAssessments(_generalConfig.Value.CallerCode);

            // Get first Open Assessment that does not exists in both OpenAssessmentsQueue and WorkflowDatabase
            foreach (var assessment in assessments)
            {
                var isExists = await
                    _dbContext.OpenAssessmentsQueue.AnyAsync(a => a.PrimarySdocId == assessment.Id);

                if (isExists)
                {
                    continue;
                }

                isExists = await
                    _dbContext.WorkflowInstance.AnyAsync(a => a.PrimarySdocId == assessment.Id);

                if (!isExists) return assessment;
            }

            return null;
        }

        private async Task AddSdocIdToQueue(int primarySdocId)
        {
            _logger.LogInformation("Adding PrimarySdocId {PrimarySdocId} to the open assessment queue");

            await _dbContext.OpenAssessmentsQueue.AddAsync(new OpenAssessmentsQueue()
            {
                PrimarySdocId = primarySdocId,
                Timestamp = DateTime.Now
            });

            await _dbContext.SaveChangesAsync();
        }
    }
}
