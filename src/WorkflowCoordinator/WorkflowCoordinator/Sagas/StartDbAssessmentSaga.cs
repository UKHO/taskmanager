using System;
using System.Threading.Tasks;
using AutoMapper;
using Common.Helpers;
using Common.Messages.Commands;
using Common.Messages.Enums;
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
using WorkflowDatabase.EF.Models;
using Task = System.Threading.Tasks.Task;

namespace WorkflowCoordinator.Sagas
{
    public class StartDbAssessmentSaga : Saga<StartDbAssessmentSagaData>,
                                            IAmStartedByMessages<StartDbAssessmentCommand>,
                                            IHandleMessages<RetrieveAssessmentDataCommand>
    {
        private readonly IOptionsSnapshot<GeneralConfig> _generalConfig;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly WorkflowDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<StartDbAssessmentSaga> _logger;

        public StartDbAssessmentSaga(IOptionsSnapshot<GeneralConfig> generalConfig,
            IDataServiceApiClient dataServiceApiClient,
            IWorkflowServiceApiClient workflowServiceApiClient,
            WorkflowDbContext dbContext, IMapper mapper,
            ILogger<StartDbAssessmentSaga> logger)
        {
            _generalConfig = generalConfig;
            _dataServiceApiClient = dataServiceApiClient;
            _workflowServiceApiClient = workflowServiceApiClient;
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<StartDbAssessmentSagaData> mapper)
        {
            mapper.ConfigureMapping<StartDbAssessmentCommand>(message => message.SourceDocumentId)
                  .ToSaga(sagaData => sagaData.SourceDocumentId);

            mapper.ConfigureMapping<RetrieveAssessmentDataCommand>(message => message.SourceDocumentId)
                .ToSaga(sagaData => sagaData.SourceDocumentId);
        }

        public async Task Handle(StartDbAssessmentCommand message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("EventName", nameof(StartDbAssessmentCommand));
            LogContext.PushProperty("ProcessId", 0);
            LogContext.PushProperty("PrimarySdocId", message.SourceDocumentId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            if (!Data.IsStarted)
            {
                Data.IsStarted = true;
                Data.CorrelationId = message.CorrelationId;
                Data.SourceDocumentId = message.SourceDocumentId;

                _logger.LogInformation("Starting new saga for PrimarySdocId {PrimarySdocId}");
            }

            if (Data.ProcessId == 0)
            {
                var workflowId = await _workflowServiceApiClient.GetDBAssessmentWorkflowId();
                Data.ProcessId = await _workflowServiceApiClient.CreateWorkflowInstance(workflowId);
            }

            LogContext.PushProperty("ProcessId", Data.ProcessId);

            var serialNumber = await _workflowServiceApiClient.GetWorkflowInstanceSerialNumber(Data.ProcessId);

            if (string.IsNullOrEmpty(serialNumber))
            {
                _logger.LogError("Failed to get K2 serial number for ProcessId {ProcessId}");
                throw new ApplicationException($"Failed to get data for K2 Task with ProcessId {Data.ProcessId}");
            }

            LogContext.PushProperty("K2SerialNumber", serialNumber);

            _logger.LogInformation("Successfully create k2 workflow instance for PrimarySdocId {PrimarySdocId} with: ProcessId {ProcessId} and K2SerialNumber {K2SerialNumber}");
            _logger.LogInformation("Saving open assessment to our system: PrimarySdocId {PrimarySdocId}, ProcessId {ProcessId}, and K2SerialNumber {K2SerialNumber}");
            var workflowInstanceId = await UpdateWorkflowInstanceTable(Data.ProcessId, Data.SourceDocumentId, serialNumber, WorkflowStatus.Started);
            await RemoveSdocIdFromQueue(Data.ProcessId);
            await UpdateDbAssessmentReviewTable(Data.ProcessId, workflowInstanceId);
            await UpdatePrimaryDocumentStatus(Data.ProcessId, Data.SourceDocumentId, Data.CorrelationId, SourceDocumentRetrievalStatus.Started);
            _logger.LogInformation("Successfully Saved open assessment to our system: PrimarySdocId {PrimarySdocId}, ProcessId {ProcessId}, and K2SerialNumber {K2SerialNumber}");

            _logger.LogInformation($"Sending {nameof(RetrieveAssessmentDataCommand)}");

            await context.SendLocal(new RetrieveAssessmentDataCommand
            {
                CorrelationId = Data.CorrelationId,
                ProcessId = Data.ProcessId,
                SourceDocumentId = Data.SourceDocumentId,
                WorkflowInstanceId = workflowInstanceId
            });

            _logger.LogInformation($"Sending {nameof(InitiateSourceDocumentRetrievalEvent)}");

            var initiateRetrievalEvent = new InitiateSourceDocumentRetrievalEvent()
            {
                CorrelationId = Data.CorrelationId,
                ProcessId = Data.ProcessId,
                SourceDocumentId = Data.SourceDocumentId,
                GeoReferenced = true,
                SourceType = SourceType.Primary,
                SdocRetrievalId = Guid.NewGuid()
            };

            await context.Publish(initiateRetrievalEvent).ConfigureAwait(false);

            ConstructAndSendLinkedDocumentRetrievalCommands(context);

            _logger.LogInformation("Successfully Completed Event {EventName}: {Message}");
        }

        /// <summary>
        /// Get assessment data from SDRA and store it in the AssessmentData table and then mark Saga Complete
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Handle(RetrieveAssessmentDataCommand message, IMessageHandlerContext context)
        {
            _logger.LogInformation($"Handling {nameof(RetrieveAssessmentDataCommand)}: {message.ToJSONSerializedString()}");

            // Call DataServices to get the assessment data for the given sdoc Id
            var assessmentData =
                await _dataServiceApiClient.GetAssessmentData(_generalConfig.Value.CallerCode,
                    message.SourceDocumentId);

            // Add assessment data and any comments to DB, after auto mapping it
            var mappedAssessmentData = _mapper.Map<DocumentAssessmentData, AssessmentData>(assessmentData);
            var mappedComments = _mapper.Map<DocumentAssessmentData, Comment>(assessmentData);

            mappedAssessmentData.ProcessId = message.ProcessId;
            mappedAssessmentData.TeamDistributedTo = null; //Set Team to null because the SDRA value is not guaranteed to be accurate
            mappedComments.WorkflowInstanceId = message.WorkflowInstanceId;
            mappedComments.ProcessId = message.ProcessId;

            // TODO: Exception triggered due to Created and Username not being populated
            mappedComments.Created = DateTime.Now;
            mappedComments.AdUserId = null;

            _dbContext.AssessmentData.Add(mappedAssessmentData);

            if (mappedComments.Text != null)
            {
                _dbContext.Comments.Add(mappedComments);
            }

            await _dbContext.SaveChangesAsync();

            MarkAsComplete();
        }

        private async Task<int> UpdateWorkflowInstanceTable(int processId, int sourceDocumentId, string serialNumber, WorkflowStatus status)
        {
            var existingInstance = await _dbContext.WorkflowInstance.FirstOrDefaultAsync(w => w.ProcessId == processId);

            if (existingInstance != null) return existingInstance.WorkflowInstanceId;

            var workflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                PrimarySdocId = sourceDocumentId,
                SerialNumber = serialNumber,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = status.ToString(),
                StartedAt = DateTime.Now,
                ActivityChangedAt = DateTime.Today
            };

            await _dbContext.WorkflowInstance.AddAsync(workflowInstance);
            await _dbContext.SaveChangesAsync();

            var newId = workflowInstance.WorkflowInstanceId;
            return newId;
        }

        private async Task RemoveSdocIdFromQueue(int primarySdocId)
        {
            var openAssessmentsQueueItem =
                await _dbContext.OpenAssessmentsQueue.SingleOrDefaultAsync(o => o.PrimarySdocId == primarySdocId);
            if (openAssessmentsQueueItem != null)
            {
                _logger.LogInformation("Removing PrimarySdocId {PrimarySdocId} from the open assessment queue");

                _dbContext.OpenAssessmentsQueue.Remove(openAssessmentsQueueItem);
                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task UpdatePrimaryDocumentStatus(int processId, int sourceDocumentId, Guid correlationId, SourceDocumentRetrievalStatus status)
        {
            var existingInstance = await _dbContext.PrimaryDocumentStatus
                                                                    .SingleOrDefaultAsync(r => r.ProcessId == processId
                                                                                                                        && r.SdocId == sourceDocumentId);

            if (existingInstance != null) return;

            var primaryDocumentStatus = new PrimaryDocumentStatus()
            {
                CorrelationId = correlationId,
                ProcessId = processId,
                SdocId = sourceDocumentId,  
                Status = status.ToString(),
                StartedAt = DateTime.Now
            };

            await _dbContext.PrimaryDocumentStatus.AddAsync(primaryDocumentStatus);
            await _dbContext.SaveChangesAsync();
        }

        private async Task UpdateDbAssessmentReviewTable(int processId, int workflowInstanceId)
        {
            var existingReviewData = await _dbContext.DbAssessmentReviewData.FirstOrDefaultAsync(w => w.ProcessId == processId);

            if (existingReviewData != null) return;

            var reviewData = new DbAssessmentReviewData
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                TaskType = "Simple"
            };

            await _dbContext.DbAssessmentReviewData.AddAsync(reviewData);
            await _dbContext.SaveChangesAsync();
        }

        private async void ConstructAndSendLinkedDocumentRetrievalCommands(IMessageHandlerContext context)
        {
            _logger.LogInformation($"Sending {nameof(GetBackwardDocumentLinksCommand)}");

            var backwardDocumentLinkCommand = new GetBackwardDocumentLinksCommand()
            {
                CorrelationId = Data.CorrelationId,
                ProcessId = Data.ProcessId,
                SourceDocumentId = Data.SourceDocumentId
            };

            await context.Send(backwardDocumentLinkCommand).ConfigureAwait(false);

            _logger.LogInformation($"Sending {nameof(GetForwardDocumentLinksCommand)}");

            var forwardDocumentLinkCommand = new GetForwardDocumentLinksCommand()
            {
                CorrelationId = Data.CorrelationId,
                ProcessId = Data.ProcessId,
                SourceDocumentId = Data.SourceDocumentId
            };

            await context.Send(forwardDocumentLinkCommand).ConfigureAwait(false);

            _logger.LogInformation($"Sending {nameof(GetSepDocumentLinksCommand)}");

            var sepDocumentLinkCommand = new GetSepDocumentLinksCommand()
            {
                CorrelationId = Data.CorrelationId,
                ProcessId = Data.ProcessId,
                SourceDocumentId = Data.SourceDocumentId
            };

            await context.Send(sepDocumentLinkCommand).ConfigureAwait(false);
        }
    }
}
