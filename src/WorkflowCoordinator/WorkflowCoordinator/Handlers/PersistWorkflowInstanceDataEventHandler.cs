using System;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using WorkflowCoordinator.HttpClients;
using WorkflowDatabase.EF;

namespace WorkflowCoordinator.Handlers
{
    public class PersistWorkflowInstanceDataEventHandler : IHandleMessages<PersistWorkflowInstanceDataEvent>
    {
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly ILogger<PersistWorkflowInstanceDataEventHandler> _logger;
        private readonly WorkflowDbContext _dbContext;

        public PersistWorkflowInstanceDataEventHandler(IWorkflowServiceApiClient workflowServiceApiClient, 
            ILogger<PersistWorkflowInstanceDataEventHandler> logger, WorkflowDbContext dbContext)
        {
            _workflowServiceApiClient = workflowServiceApiClient;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Handle(PersistWorkflowInstanceDataEvent message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(PersistWorkflowInstanceDataEvent));
            LogContext.PushProperty("CorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);
            LogContext.PushProperty("FromActivityName", message.FromActivityName);
            LogContext.PushProperty("ToActivityName", message.ToActivityName);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            var k2Task = await _workflowServiceApiClient.GetWorkflowInstanceData(message.ProcessId);

            if (k2Task.ActivityName != message.ToActivityName)
            {
                _logger.LogError("K2Task at stage {K2Stage} is not at {ToActivityName}", k2Task.ActivityName);
                throw new ApplicationException($"K2Task at stage {k2Task.ActivityName} is not at {message.ToActivityName}");
            }

            var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(wi => wi.ProcessId == message.ProcessId);

            workflowInstance.SerialNumber = k2Task.SerialNumber;
            workflowInstance.ActivityName = k2Task.ActivityName;
            workflowInstance.Status = WorkflowStatus.Started.ToString();

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully Completed Event {EventName}: {Message}");

        }
    }
}
