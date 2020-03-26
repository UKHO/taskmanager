using System;
using System.Threading.Tasks;
using Common.Factories;
using Common.Factories.Interfaces;
using Microsoft.Extensions.Options;
using NServiceBus;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.Enums;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;
using WorkflowDatabase.EF;

namespace SourceDocumentCoordinator.Handlers
{
    public class ClearDocumentRequestFromQueueCommandHandler : IHandleMessages<ClearDocumentRequestFromQueueCommand>
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IOptionsSnapshot<GeneralConfig> _generalConfig;
        private readonly IDocumentStatusFactory _documentStatusFactory;

        public ClearDocumentRequestFromQueueCommandHandler(IDataServiceApiClient dataServiceApiClient,
            WorkflowDbContext dbContext, IOptionsSnapshot<GeneralConfig> generalConfig, IDocumentStatusFactory documentStatusFactory)
        {
            _dataServiceApiClient = dataServiceApiClient;
            _dbContext = dbContext;
            _generalConfig = generalConfig;
            _documentStatusFactory = documentStatusFactory;
        }

        public async Task Handle(ClearDocumentRequestFromQueueCommand message, IMessageHandlerContext context)
        {
            var returnCode = await _dataServiceApiClient
                .DeleteDocumentRequestJobFromQueue(
                                                    _generalConfig.Value.CallerCode,
                                                    message.SourceDocumentId,
                                                    _generalConfig.Value.SourceDocumentWriteableFolderName)
                .ConfigureAwait(false);

            if (returnCode.Code.HasValue)
            {
                switch (returnCode.Code.Value)
                {
                    case (int)ClearFromQueueReturnCodeEnum.Success:
                    case (int)ClearFromQueueReturnCodeEnum.Warning:
                        await SourceDocumentHelper.UpdateSourceDocumentStatus(
                                                    _documentStatusFactory, 
                                                    message.ProcessId, 
                                                    message.SourceDocumentId, null, null, 
                                                    SourceDocumentRetrievalStatus.Complete, 
                                                    message.SourceType, message.CorrelationId);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
