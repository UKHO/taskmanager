using Common.Helpers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NServiceBus;
using NServiceBus.Logging;

using System;

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

        ILog log = LogManager.GetLogger<StartDbAssessmentSaga>();

        public StartDbAssessmentSaga(IOptionsSnapshot<GeneralConfig> generalConfig,
            IDataServiceApiClient dataServiceApiClient,
            IWorkflowServiceApiClient workflowServiceApiClient,
            WorkflowDbContext dbContext)
        {
            _generalConfig = generalConfig;
            _dataServiceApiClient = dataServiceApiClient;
            _workflowServiceApiClient = workflowServiceApiClient;
            _dbContext = dbContext;
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

            await UpdateWorkflowInstanceTable(Data.ProcessId, serialNumber, WorkflowStatus.Started);

            // TODO: Fire message to get SDRA data from SDRA-DB and store it in WorkflowDatabase
            log.Debug($"Sending {nameof(RetrieveAssessmentDataCommand)}");
            await context.Send(new RetrieveAssessmentDataCommand
            {
                CorrelationId = Data.CorrelationId,
                SourceDocumentId = Data.SourceDocumentId
            });

            log.Debug($"Finished handling {nameof(StartDbAssessmentCommand)}");
        }

        public async Task Handle(RetrieveAssessmentDataCommand message, IMessageHandlerContext context)
        {
            // TODO: Get SDRA data from SDRA-DB and store it in WorkflowDatabase; and then mark Saga Complete
            // Use Saga data to get Sdocid and processId
            log.Debug($"Handling {nameof(RetrieveAssessmentDataCommand)}: {message.ToJSONSerializedString()}");

            MarkAsComplete();
        }

        private async Task UpdateWorkflowInstanceTable(int processId, string serialNumber, WorkflowStatus status)
        {
            var isExist= await _dbContext.WorkflowInstance.AnyAsync(w => w.ProcessId == processId);

            if (!isExist)
            {

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
            }

        }
    }
}
