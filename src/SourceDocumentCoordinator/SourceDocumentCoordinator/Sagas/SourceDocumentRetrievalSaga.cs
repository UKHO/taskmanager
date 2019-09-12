using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Commands;
using NServiceBus;
using NServiceBus.Logging;
using WorkflowDatabase.EF;

namespace SourceDocumentCoordinator.Sagas
{
    public class SourceDocumentRetrievalSaga : Saga<SourceDocumentRetrievalSagaData>, IAmStartedByMessages<InitiateSourceDocumentRetrievalCommand>
    {
        private readonly WorkflowDbContext _dbContext;
        ILog log = LogManager.GetLogger<SourceDocumentRetrievalSaga>();

        public SourceDocumentRetrievalSaga(WorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SourceDocumentRetrievalSagaData> mapper)
        {
            mapper.ConfigureMapping<InitiateSourceDocumentRetrievalCommand>(message => message.SourceDocumentId)
                .ToSaga(sagaData => sagaData.SourceDocumentId);
        }

        public Task Handle(InitiateSourceDocumentRetrievalCommand message, IMessageHandlerContext context)
        {
            log.Debug($"Handling {nameof(InitiateSourceDocumentRetrievalCommand)}: {message.ToJSONSerializedString()}");

            // Call GetDocumentForViewing method on DataServices API

            // Send command to check GetDocumentRequestQueueStatus on DataServices API

            // Once document has been fetched, call ClearDocumentRequestJobFromQueue on DataServices API and close saga...
            
            return null;
        }
    }
}
