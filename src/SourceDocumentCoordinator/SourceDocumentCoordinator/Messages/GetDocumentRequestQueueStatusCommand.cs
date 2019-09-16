using System;
using System.Collections.Generic;
using System.Text;
using Common.Messages;

namespace SourceDocumentCoordinator.Messages
{
    public class GetDocumentRequestQueueStatusCommand : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int SdocId { get; set; }
    }
}
