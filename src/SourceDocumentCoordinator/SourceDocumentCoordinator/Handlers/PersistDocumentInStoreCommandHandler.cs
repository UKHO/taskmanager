using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;
using WorkflowDatabase.EF;

namespace SourceDocumentCoordinator.Handlers
{
    public class PersistDocumentInStoreCommandHandler : IHandleMessages<PersistDocumentInStoreCommand>
    {
        private readonly ISourceDocumentServiceApiClient _sourceDocumentServiceApiClient;
        private readonly WorkflowDbContext _dbContext;
        private readonly ILogger<PersistDocumentInStoreCommandHandler> _logger;
        private readonly IFileSystem _fileSystem;

        public PersistDocumentInStoreCommandHandler(ISourceDocumentServiceApiClient sourceDocumentServiceApiClient,
                                                    WorkflowDbContext dbContext,
                                                    ILogger<PersistDocumentInStoreCommandHandler> logger,
                                                    IFileSystem fileSystem)
        {
            _sourceDocumentServiceApiClient = sourceDocumentServiceApiClient;
            _dbContext = dbContext;
            _logger = logger;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Read file from SDRA output folder, post to Content Service and store returned Guid in db.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Handle(PersistDocumentInStoreCommand message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("EventName", nameof(PersistDocumentInStoreCommand));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("SourceDocumentPath", message.Filepath);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            _logger.LogInformation("Calling SourceDocumentService to persist file: {SourceDocumentPath}");

            var contentServiceId = await _sourceDocumentServiceApiClient.Post(message.ProcessId,
                message.SourceDocumentId, Path.GetFileName(message.Filepath));

            LogContext.PushProperty("ContentServiceId", contentServiceId);

            _logger.LogInformation("Successfully called SourceDocumentService to post document to Content Service with Content Service Id {ContentServiceId}");

            await UpdatePrimaryDocumentStatus(message, contentServiceId);
            await UpdateLinkedDocument(message, contentServiceId, message.UniqueId);
            await UpdateDatabaseDocuments(message, contentServiceId);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Completed {EventName} handler with: {Message}");

        }

        private async Task UpdatePrimaryDocumentStatus(PersistDocumentInStoreCommand message, Guid newGuid)
        {

            _logger.LogInformation("Updating PrimaryDocumentStatus with generated source document file info");

            var primaryDocuments = await _dbContext.PrimaryDocumentStatus
                                                                .Where(pd => pd.SdocId == message.SourceDocumentId
                                                                             && (pd.Status == SourceDocumentRetrievalStatus.Started.ToString()
                                                                                 || pd.Status == SourceDocumentRetrievalStatus.Ready.ToString()))
                                                                .ToListAsync();

            foreach (var primaryDocument in primaryDocuments)
            {
                primaryDocument.ContentServiceId = newGuid;
                primaryDocument.Filename = Path.GetFileName(message.Filepath)?.Trim();
                primaryDocument.Filepath = Path.GetDirectoryName(message.Filepath)?.Trim();
                primaryDocument.Status = SourceDocumentRetrievalStatus.FileGenerated.ToString();
            }
        }

        private async Task UpdateLinkedDocument(PersistDocumentInStoreCommand message, Guid newGuid, Guid uniqueId)
        {

            _logger.LogInformation("Updating LinkedDocument with generated source document file info");

            var linkedDocument = await _dbContext.LinkedDocument
                                                                .Where(pd => pd.UniqueId == uniqueId
                                                                             && (pd.Status == SourceDocumentRetrievalStatus.Started.ToString()
                                                                                 || pd.Status == SourceDocumentRetrievalStatus.Ready.ToString()))
                                                                .SingleOrDefaultAsync();
            linkedDocument.ContentServiceId = newGuid;
            linkedDocument.Filename = Path.GetFileName(message.Filepath)?.Trim();
            linkedDocument.Filepath = Path.GetDirectoryName(message.Filepath)?.Trim();
            linkedDocument.Status = SourceDocumentRetrievalStatus.FileGenerated.ToString();
        }

        private async Task UpdateDatabaseDocuments(PersistDocumentInStoreCommand message, Guid newGuid)
        {
            _logger.LogInformation("Updating DatabaseDocumentStatus with generated source document file info");

            var databaseDocumentStatuses = await _dbContext.DatabaseDocumentStatus
                .Where(pd => pd.SdocId == message.SourceDocumentId
                             && (pd.Status == SourceDocumentRetrievalStatus.Started.ToString()
                                 || pd.Status == SourceDocumentRetrievalStatus.Ready.ToString()))
                .ToListAsync();

            foreach (var databaseDocumentStatus in databaseDocumentStatuses)
            {
                databaseDocumentStatus.ContentServiceId = newGuid;
                databaseDocumentStatus.Filename = Path.GetFileName(message.Filepath)?.Trim();
                databaseDocumentStatus.Filepath = Path.GetDirectoryName(message.Filepath)?.Trim();
                databaseDocumentStatus.Status = SourceDocumentRetrievalStatus.FileGenerated.ToString();
            }
        }
    }
}
