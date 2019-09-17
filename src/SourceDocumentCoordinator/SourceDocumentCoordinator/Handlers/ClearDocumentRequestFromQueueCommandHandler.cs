using System;
using System.Threading;
using System.Threading.Tasks;
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

        public ClearDocumentRequestFromQueueCommandHandler(IDataServiceApiClient dataServiceApiClient,
            WorkflowDbContext dbContext, IOptionsSnapshot<GeneralConfig> generalConfig)
        {
            _dataServiceApiClient = dataServiceApiClient;
            _dbContext = dbContext;
            _generalConfig = generalConfig;
        }

        public async Task Handle(ClearDocumentRequestFromQueueCommand message, IMessageHandlerContext context)
        {
            var returnCode = _dataServiceApiClient
                .DeleteDocumentRequestJobFromQueue(_generalConfig.Value.CallerCode, message.SdocId, ConstructWriteableFolderName(message.SdocId));

            if (returnCode.Result.Code.HasValue)
            {
                switch (returnCode.Result.Code.Value)
                {
                    case (int)ClearDocumentFromQueueReturnCodeEnum.Success:
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private string ConstructWriteableFolderName(int sdocId)
        {
            return "";
        }
    }
}
