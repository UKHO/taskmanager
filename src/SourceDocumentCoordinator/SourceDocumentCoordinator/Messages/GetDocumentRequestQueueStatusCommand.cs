using System;
using Common.Messages;
using Common.Messages.Enums;

namespace SourceDocumentCoordinator.Messages
{
    public class GetDocumentRequestQueueStatusCommand : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int SourceDocumentId { get; set; }
        public SourceType SourceType { get; set; }
        public Guid SdocRetrievalId { get; set; }
    }
}
