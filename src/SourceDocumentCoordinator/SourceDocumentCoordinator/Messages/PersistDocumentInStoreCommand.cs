using System;
using Common.Messages;

namespace SourceDocumentCoordinator.Messages
{
    public class PersistDocumentInStoreCommand : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int SdocId { get; set; }
    }
}
