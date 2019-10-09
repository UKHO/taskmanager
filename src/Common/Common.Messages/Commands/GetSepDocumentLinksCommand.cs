using System;

namespace Common.Messages.Commands
{
    public class GetSepDocumentLinksCommand : ICorrelate
    {
        public int SourceDocumentId { get; set; }
        public Guid CorrelationId { get; set; }
        public int ProcessId { get; set; }
    }
}
