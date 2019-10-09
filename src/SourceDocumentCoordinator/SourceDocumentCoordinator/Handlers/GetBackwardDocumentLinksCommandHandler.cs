using System.Threading.Tasks;
using Common.Messages.Commands;
using NServiceBus;

namespace SourceDocumentCoordinator.Handlers
{
    public class GetBackwardDocumentLinksCommandHandler : IHandleMessages<GetBackwardDocumentLinksCommand>
    {
        public Task Handle(GetBackwardDocumentLinksCommand message, IMessageHandlerContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
