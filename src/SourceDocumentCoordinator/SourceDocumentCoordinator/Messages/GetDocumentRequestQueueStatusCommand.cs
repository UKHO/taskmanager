using System;
using Common.Messages;

namespace SourceDocumentCoordinator.Messages
{
    public class GetDocumentRequestQueueStatusCommand : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int SourceDocumentId { get; set; }
    }
}
