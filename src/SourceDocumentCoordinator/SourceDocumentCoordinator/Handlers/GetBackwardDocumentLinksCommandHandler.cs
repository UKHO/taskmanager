using System.Threading.Tasks;
using Common.Messages.Commands;
using NServiceBus;
using SourceDocumentCoordinator.HttpClients;

namespace SourceDocumentCoordinator.Handlers
{
    public class GetBackwardDocumentLinksCommandHandler : IHandleMessages<GetBackwardDocumentLinksCommand>
    {
        private readonly IDataServiceApiClient _dataServiceApiClient;

        public GetBackwardDocumentLinksCommandHandler(IDataServiceApiClient dataServiceApiClient)
        {
            _dataServiceApiClient = dataServiceApiClient;
        }

        public async Task Handle(GetBackwardDocumentLinksCommand message, IMessageHandlerContext context)
        {
            var docLinks = await _dataServiceApiClient.GetBackwardDocumentLinks(message.SourceDocumentId);
        }
    }
}
