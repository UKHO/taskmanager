using System;

namespace Common.Messages
{
    public class InitiateSourceDocumentRetrievalCommand : ICorrelate
    {
        public int SourceDocumentId { get; set; }
        public Guid CorrelationId { get; set; }
    }
}
