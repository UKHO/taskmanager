using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NServiceBus;
using NServiceBus.Logging;
using System;
using System.Linq;
using AutoMapper;
using DataServices.Models;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;
using Task = System.Threading.Tasks.Task;
using System.Threading.Tasks;
using Common.Messages.Commands;

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

        ILog log = LogManager.GetLogger<StartDbAssessmentSaga>();

        public StartDbAssessmentSaga(IOptionsSnapshot<GeneralConfig> generalConfig,
            IDataServiceApiClient dataServiceApiClient,
            IWorkflowServiceApiClient workflowServiceApiClient,
            WorkflowDbContext dbContext, IMapper mapper)
        {
            _generalConfig = generalConfig;
            _dataServiceApiClient = dataServiceApiClient;
            _workflowServiceApiClient = workflowServiceApiClient;
            _dbContext = dbContext;
            _mapper = mapper;
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
            log.Debug($"Handling {nameof(StartDbAssessmentCommand)}: {message.ToJSONSerializedString()}");

            if (!Data.IsStarted)
            {
                Data.IsStarted = true;
                Data.CorrelationId = message.CorrelationId;
                Data.SourceDocumentId = message.SourceDocumentId;

                log.Debug($"Saved {Data.ToJSONSerializedString()} " +
                          $"to {nameof(StartDbAssessmentSagaData)}");
            }

            if (Data.ProcessId == 0)
            {
                var workflowId = await _workflowServiceApiClient.GetDBAssessmentWorkflowId();
                Data.ProcessId = await _workflowServiceApiClient.CreateWorkflowInstance(workflowId);
            }

            var serialNumber = await _workflowServiceApiClient.GetWorkflowInstanceSerialNumber(Data.ProcessId);

            var workflowInstanceId = await UpdateWorkflowInstanceTable(Data.ProcessId, serialNumber, WorkflowStatus.Started);

            log.Debug($"Sending {nameof(RetrieveAssessmentDataCommand)}");
            await context.SendLocal(new RetrieveAssessmentDataCommand
            {
                CorrelationId = Data.CorrelationId,
                ProcessId = Data.ProcessId,
                SourceDocumentId = Data.SourceDocumentId,
                WorkflowInstanceId = workflowInstanceId.Value
            });

            log.Debug($"Sending {nameof(InitiateSourceDocumentRetrievalCommand)}");

            var initiateRetrievalCommand = new InitiateSourceDocumentRetrievalCommand()
            {
                CorrelationId = Data.CorrelationId,
                ProcessId = Data.ProcessId,
                SourceDocumentId = Data.SourceDocumentId,
                GeoReferenced = true
            };

            await context.Send(initiateRetrievalCommand).ConfigureAwait(false);

            ConstructAndSendLinkedDocumentRetrievalCommands(context);

            log.Debug($"Finished handling {nameof(StartDbAssessmentCommand)}");
        }

        /// <summary>
        /// Get assessment data from SDRA and store it in the AssessmentData table and then mark Saga Complete
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Handle(RetrieveAssessmentDataCommand message, IMessageHandlerContext context)
        {
            log.Debug($"Handling {nameof(RetrieveAssessmentDataCommand)}: {message.ToJSONSerializedString()}");

            // Call DataServices to get the assessment data for the given sdoc Id
            var assessmentData =
                await _dataServiceApiClient.GetAssessmentData(_generalConfig.Value.CallerCode,
                    message.SourceDocumentId);

            // Add assessment data and any comments to DB, after auto mapping it
            var mappedAssessmentData = _mapper.Map<DocumentAssessmentData, AssessmentData>(assessmentData);
            var mappedComments = _mapper.Map<DocumentAssessmentData, Comments>(assessmentData);

            mappedAssessmentData.ProcessId = message.ProcessId;
            mappedComments.WorkflowInstanceId = message.WorkflowInstanceId;
            mappedComments.ProcessId = message.ProcessId;

            // TODO: Exception triggered due to Created and Username not being populated
            mappedComments.Created = DateTime.Now;
            mappedComments.Username = string.Empty;

            _dbContext.AssessmentData.Add(mappedAssessmentData);

            if (mappedComments.Text != null)
            {
                _dbContext.Comment.Add(mappedComments);
            }

            await _dbContext.SaveChangesAsync();

            // TODO: Fire message to create the first 'Assigned Task' in the new Tasks table and move MarkAsComplete to its handler

            MarkAsComplete();
        }

        private async Task<int?> UpdateWorkflowInstanceTable(int processId, string serialNumber, WorkflowStatus status)
        {
            var existingInstance = await _dbContext.WorkflowInstance.FirstOrDefaultAsync(w => w.ProcessId == processId);

            if (existingInstance != null) return existingInstance.WorkflowInstanceId;

            var workflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                SerialNumber = serialNumber,
                WorkflowType = WorkflowConstants.WorkflowType,
                ActivityName = WorkflowConstants.ActivityName,
                Status = status.ToString(),
                StartedAt = DateTime.Now
            };

            await _dbContext.WorkflowInstance.AddAsync(workflowInstance);
            await _dbContext.SaveChangesAsync();

            var newId = workflowInstance.WorkflowInstanceId;
            return newId;
        }

        private async void ConstructAndSendLinkedDocumentRetrievalCommands(IMessageHandlerContext context)
        {
            var backwardDocumentLinkCommand = new GetBackwardDocumentLinksCommand()
            {
                CorrelationId = Data.CorrelationId,
                ProcessId = Data.ProcessId,
                SourceDocumentId = Data.SourceDocumentId
            };

            await context.Send(backwardDocumentLinkCommand).ConfigureAwait(false);

            var forwardDocumentLinkCommand = new GetForwardDocumentLinksCommand()
            {
                CorrelationId = Data.CorrelationId,
                ProcessId = Data.ProcessId,
                SourceDocumentId = Data.SourceDocumentId
            };

            await context.Send(forwardDocumentLinkCommand).ConfigureAwait(false);

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
