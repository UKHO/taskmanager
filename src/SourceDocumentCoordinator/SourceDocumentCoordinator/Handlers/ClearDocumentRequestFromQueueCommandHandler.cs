using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NServiceBus;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.Enums;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

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
                        UpdateSourceDocumentStatus(message.SourceDocumentId, SourceDocumentRetrievalStatus.Complete);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private async void UpdateSourceDocumentStatus(int sdocId, SourceDocumentRetrievalStatus status)
        {
            var row = await _dbContext.PrimaryDocumentStatus.FirstAsync(s => s.SdocId == sdocId);
            row.Status = status.ToString();
            await _dbContext.SaveChangesAsync();
        }
    }
}
