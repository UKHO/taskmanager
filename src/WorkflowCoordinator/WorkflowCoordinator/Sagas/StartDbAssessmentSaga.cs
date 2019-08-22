using System;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.Extensions.Options;
using NServiceBus;
using NServiceBus.Logging;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowDatabase.EF;

namespace WorkflowCoordinator.Sagas
{
    public class StartDbAssessmentSaga : Saga<StartDbAssessmentSagaData>,
            IAmStartedByMessages<StartDbAssessmentCommand>
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

            if (Data.ProcessId.Equals(0))
            {
                //TODO: Start Workflow

                //TODO: Save K2 ProcessId to SagaData

            }

            //TODO: Get Serial Number and relevant WorkflowInstance data from K2






            //TODO: Save WorkflowInstance in WorkflowDatabase

            log.Debug($"Sending {nameof(RetrieveAssessmentDataCommand)}");
            await context.Send(new RetrieveAssessmentDataCommand
            {
                CorrelationId = Data.CorrelationId,
                SourceDocumentId = Data.SourceDocumentId
            });

            log.Debug($"Finished handling {nameof(StartDbAssessmentCommand)}");
        }
    }
}
