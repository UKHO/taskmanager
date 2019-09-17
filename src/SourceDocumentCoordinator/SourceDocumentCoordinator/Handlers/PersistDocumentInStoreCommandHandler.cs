using System;
using System.Threading.Tasks;
using NServiceBus;
using SourceDocumentCoordinator.Messages;

namespace SourceDocumentCoordinator.Handlers
{
    public class PersistDocumentInStoreCommandHandler : IHandleMessages<PersistDocumentInStoreCommand>
    {
        public Task Handle(PersistDocumentInStoreCommand message, IMessageHandlerContext context)
        {
            // TODO: Retrieve doc from file share, and post to content service
            throw new NotImplementedException();
        }
    }
}
