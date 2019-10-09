using System;
using System.Threading.Tasks;
using Common.Messages.Commands;
using NServiceBus;

namespace SourceDocumentCoordinator.Handlers
{
    public class GetSepDocumentLinksCommandHandler : IHandleMessages<GetSepDocumentLinksCommand>
    {
        public Task Handle(GetSepDocumentLinksCommand message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
}
