using System;
using Common.Messages;
using NServiceBus;

namespace SourceDocumentCoordinator.Sagas
{
    public class SourceDocumentRetrievalSagaData : ContainSagaData, ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int SourceDocumentId { get; set; }
    }
}
