using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.Handlers
{
    public class CompleteAssessmentCommandHandler : IHandleMessages<CompleteAssessmentCommand>
    {
        private readonly ILogger<CompleteAssessmentCommandHandler> _logger;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly WorkflowDbContext _dbContext;

        public CompleteAssessmentCommandHandler(
                                                ILogger<CompleteAssessmentCommandHandler> logger,
                                                IDataServiceApiClient dataServiceApiClient,
                                                WorkflowDbContext dbContext)
        {
            _logger = logger;
            _dataServiceApiClient = dataServiceApiClient;
            _dbContext = dbContext;
        }

        public async Task Handle(CompleteAssessmentCommand message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(CompleteAssessmentCommand));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            var workflowInstance = await _dbContext.WorkflowInstance
                .Include(wi => wi.AssessmentData)
                .Include(wi => wi.DbAssessmentVerifyData)
                .FirstOrDefaultAsync(wi => wi.ProcessId == message.ProcessId);

            var action =
                workflowInstance.DbAssessmentVerifyData.TaskType.Equals("Simple",
                    StringComparison.InvariantCultureIgnoreCase)
                    ? "Imm Act - NM"
                    : "Longer-term Action";
            var sdocId = workflowInstance.AssessmentData.PrimarySdocId;

            // TODO: Mark SDRA Assessment as Assessed first, then mark as completed

            await UpdateSdraAssessmentAsAssessed(sdocId, message.ProcessId, action);
            await UpdateSdraAssessmentAsCompleted(sdocId, action);
        }

        private async Task UpdateSdraAssessmentAsAssessed(int sdocId, int processId, string action)
        {
            // TODO: Mark SDRA Assessment as Assessed first, then mark as completed


            try
            {
                //await _dataServiceApiClient.MarkAssessmentAsCompleted(workflowInstance.AssessmentData.PrimarySdocId,
                //    comment);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed requesting DataService {DataServiceResource} with: PrimarySdocId: {PrimarySdocId}",
                    nameof(_dataServiceApiClient.MarkAssessmentAsCompleted),
                    sdocId);
            }
        }

        private async Task UpdateSdraAssessmentAsCompleted(int sdocId, string comment)
        {
            // TODO: Mark SDRA Assessment as Assessed first, then mark as completed
            try
            {
                await _dataServiceApiClient.MarkAssessmentAsCompleted(sdocId, comment);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed requesting DataService {DataServiceResource} with: PrimarySdocId: {PrimarySdocId}; Comment: {Comment};",
                    nameof(_dataServiceApiClient.MarkAssessmentAsCompleted),
                    sdocId,
                    comment);
            }
        }
    }
}
