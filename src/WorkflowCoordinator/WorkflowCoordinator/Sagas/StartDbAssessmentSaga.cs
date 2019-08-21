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
    public class StartDbAssessmentSaga : Saga<StartDbAssessmentSagaData>,
            IAmStartedByMessages<StartDbAssessmentCommand>
    {
        private readonly IOptionsSnapshot<GeneralConfig> _generalConfig;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly WorkflowDbContext _dbContext;

        ILog log = LogManager.GetLogger<StartDbAssessmentSaga>();

        public StartDbAssessmentSaga(IOptionsSnapshot<GeneralConfig> generalConfig,
            IDataServiceApiClient dataServiceApiClient, WorkflowDbContext dbContext)
        {
            _generalConfig = generalConfig;
            _dataServiceApiClient = dataServiceApiClient;
            _dbContext = dbContext;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<StartDbAssessmentSagaData> mapper)
        {
            mapper.ConfigureMapping<StartDbAssessmentCommand>(message => message.SourceDocumentId)
                  .ToSaga(sagaData => sagaData.SourceDocumentId);
        }

        public async Task Handle(StartDbAssessmentCommand message, IMessageHandlerContext context)
        {
            log.Debug($"Handling {nameof(StartDbAssessmentCommand)}");

            if (Completed)
            {
                //TODO: Is this the behaviour we want?
                throw new ArgumentException($"{nameof(StartDbAssessmentSaga)} is already complete for " +
                                            $"{nameof(message.CorrelationId)}: {message.CorrelationId}" +
                                            $"{nameof(message.SourceDocumentId)}: {message.SourceDocumentId}");
            }

            Data.CorrelationId = message.CorrelationId;
            Data.SourceDocumentId = message.SourceDocumentId;

            log.Debug($"Saved {nameof(Data.CorrelationId)}:{Data.CorrelationId}; " +
                      $"{nameof(Data.SourceDocumentId)}:{Data.SourceDocumentId}; " +
                      $"to {nameof(StartDbAssessmentSagaData)}");

            //TODO: Start Workflow

            //TODO: Save WorkflowInstanceID to SagaData

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
