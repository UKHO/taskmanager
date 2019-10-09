using System.Threading.Tasks;
using Common.Messages.Commands;
using NServiceBus;
using SourceDocumentCoordinator.HttpClients;

namespace SourceDocumentCoordinator.Handlers
{
    public class GetSepDocumentLinksCommandHandler : IHandleMessages<GetSepDocumentLinksCommand>
    {
        private readonly IDataServiceApiClient _dataServiceApiClient;

        public GetSepDocumentLinksCommandHandler(IDataServiceApiClient dataServiceApiClient)
        {
            _dataServiceApiClient = dataServiceApiClient;
        }
        public async Task Handle(GetSepDocumentLinksCommand message, IMessageHandlerContext context)
        {
            var docObjects = await _dataServiceApiClient.GetSepDocumentLinks(message.SourceDocumentId);
        }
    }
}
