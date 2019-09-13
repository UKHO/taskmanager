using System;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Commands;
using Microsoft.Extensions.Options;
using NServiceBus;
using NServiceBus.Logging;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.HttpClients;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace SourceDocumentCoordinator.Sagas
{
    public class SourceDocumentRetrievalSaga : Saga<SourceDocumentRetrievalSagaData>, IAmStartedByMessages<InitiateSourceDocumentRetrievalCommand>
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IOptionsSnapshot<GeneralConfig> _generalConfig;
        ILog log = LogManager.GetLogger<SourceDocumentRetrievalSaga>();

        public SourceDocumentRetrievalSaga(WorkflowDbContext dbContext, IDataServiceApiClient dataServiceApiClient,
            IOptionsSnapshot<GeneralConfig> generalConfig)
        {
            _dbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _generalConfig = generalConfig;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SourceDocumentRetrievalSagaData> mapper)
        {
            mapper.ConfigureMapping<InitiateSourceDocumentRetrievalCommand>(message => message.SourceDocumentId)
                .ToSaga(sagaData => sagaData.SourceDocumentId);
        }

        public async Task Handle(InitiateSourceDocumentRetrievalCommand message, IMessageHandlerContext context)
        {
            log.Debug($"Handling {nameof(InitiateSourceDocumentRetrievalCommand)}: {message.ToJSONSerializedString()}");

            // Call GetDocumentForViewing method on DataServices API
           var returnCode =  await _dataServiceApiClient.GetDocumentForViewing(_generalConfig.Value.CallerCode, message.SourceDocumentId, "d",
                true);

           // TODO: Think about different return code scenarios
           if (returnCode.Code.HasValue && returnCode.Code.Value == 0)
           {
               _dbContext.SourceDocumentStatus.Add(new SourceDocumentStatus
               {
                   ProcessId = message.ProcessId,
                   SdocId = message.SourceDocumentId,
                   Status = "Started",
                   StartedAt = DateTime.Now
               });

               _dbContext.SaveChanges();
           }

           // TODO: Subsequent stories:
           // 1). Send command to check GetDocumentRequestQueueStatus on DataServices API
           // 2). Once document has been fetched, call ClearDocumentRequestJobFromQueue on DataServices API and close saga...

        }
    }
}
