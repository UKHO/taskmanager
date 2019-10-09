using System;
using System.Threading.Tasks;
using Common.Messages.Commands;
using NServiceBus;

namespace SourceDocumentCoordinator.Handlers
{
    public class GetForwardDocumentLinksCommandHandler : IHandleMessages<GetForwardDocumentLinksCommand>
    {
        public Task Handle(GetForwardDocumentLinksCommand message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
}
