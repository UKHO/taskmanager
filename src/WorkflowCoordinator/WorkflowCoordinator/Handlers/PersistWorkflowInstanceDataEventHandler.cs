using System;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Events;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using WorkflowCoordinator.HttpClients;

namespace WorkflowCoordinator.Handlers
{
    public class PersistWorkflowInstanceDataEventHandler : IHandleMessages<PersistWorkflowInstanceDataEvent>
    {
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly ILogger<PersistWorkflowInstanceDataEventHandler> _logger;

        public PersistWorkflowInstanceDataEventHandler(IWorkflowServiceApiClient workflowServiceApiClient, 
            ILogger<PersistWorkflowInstanceDataEventHandler> logger)
        {
            _workflowServiceApiClient = workflowServiceApiClient;
            _logger = logger;
        }

        public async Task Handle(PersistWorkflowInstanceDataEvent message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("EventName", nameof(PersistWorkflowInstanceDataEvent));
            LogContext.PushProperty("CorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);
            LogContext.PushProperty("FromActivityName", message.FromActivityName);
            LogContext.PushProperty("ToActivityName", message.ToActivityName);
            _logger.LogInformation("Entering {EventName} handler with: {Message}", message.ToJSONSerializedString());

            var k2Task = await _workflowServiceApiClient.GetWorkflowInstanceData(message.ProcessId);

            if (k2Task.ActivityName != message.ToActivityName)
            {
                _logger.LogError("K2Task at stage {K2Stage} is not at {ToActivityName}", k2Task.ActivityName);
                throw new ApplicationException($"K2Task at stage {k2Task.ActivityName} is not at Assess");
            }

            //workflowInstance.SerialNumber = k2Task.SerialNumber;
            //workflowInstance.ActivityName = k2Task.ActivityName;
            //await _dbContext.SaveChangesAsync();
        }
    }
}
