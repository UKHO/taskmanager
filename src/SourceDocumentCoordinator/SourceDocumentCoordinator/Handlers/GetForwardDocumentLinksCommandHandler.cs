using System.Threading.Tasks;
using Common.Messages.Commands;
using NServiceBus;
using SourceDocumentCoordinator.HttpClients;

namespace SourceDocumentCoordinator.Handlers
{
    public class GetForwardDocumentLinksCommandHandler : IHandleMessages<GetForwardDocumentLinksCommand>
    {
        private readonly IDataServiceApiClient _dataServiceApiClient;

        public GetForwardDocumentLinksCommandHandler(IDataServiceApiClient dataServiceApiClient)
        {

            _dataServiceApiClient = dataServiceApiClient;
        }
        public async Task Handle(GetForwardDocumentLinksCommand message, IMessageHandlerContext context)
        {
            var docLinks = await _dataServiceApiClient.GetForwardDocumentLinks(message.SourceDocumentId);
        }
    }
}
