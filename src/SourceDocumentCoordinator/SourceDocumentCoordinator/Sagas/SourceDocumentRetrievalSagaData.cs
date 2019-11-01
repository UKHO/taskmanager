using System;
using Common.Messages;
using Common.Messages.Enums;
using NServiceBus;

namespace SourceDocumentCoordinator.Sagas
{
    public class SourceDocumentRetrievalSagaData : ContainSagaData, ICorrelate
    {
        public bool IsStarted { get; set; }  
        public Guid CorrelationId { get; set; }
        public int SourceDocumentId { get; set; }
        public int DocumentStatusId { get; set; } 
        public int ProcessId { get; set; }
        public SourceDocumentType DocumentType { get; set; }
    }
}
