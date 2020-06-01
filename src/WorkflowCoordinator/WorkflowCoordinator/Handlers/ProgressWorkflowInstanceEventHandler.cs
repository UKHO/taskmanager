using System;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Events;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowCoordinator.Models;
using WorkflowDatabase.EF;

namespace WorkflowCoordinator.Handlers
{
    public class ProgressWorkflowInstanceEventHandler : IHandleMessages<ProgressWorkflowInstanceEvent>
    {
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly ILogger<ProgressWorkflowInstanceEventHandler> _logger;

        public ProgressWorkflowInstanceEventHandler(IWorkflowServiceApiClient workflowServiceApiClient,
            ILogger<ProgressWorkflowInstanceEventHandler> logger)
        {
            _workflowServiceApiClient = workflowServiceApiClient;
            _logger = logger;
        }

        public async Task Handle(ProgressWorkflowInstanceEvent message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(ProgressWorkflowInstanceEvent));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);
            LogContext.PushProperty("FromActivity", message.FromActivity);
            LogContext.PushProperty("ToActivity", message.ToActivity);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            var k2Task = await _workflowServiceApiClient.GetWorkflowInstanceData(message.ProcessId);

            ValidateK2TaskPriorToProgressing(message, k2Task);

            LogContext.PushProperty("SerialNumber", k2Task.SerialNumber);

            var success = false;

            switch (message.ToActivity)
            {
                case WorkflowStage.Terminated:
                    // Terminate Review
                    _logger.LogInformation("Starting terminating k2 task with ProcessId {ProcessId} and SerialNumber {SerialNumber}");
                    await UpdateK2WorkflowAsTerminated(k2Task.SerialNumber);
                    _logger.LogInformation("Successfully completed the request to terminate k2 task with ProcessId {ProcessId} and SerialNumber {SerialNumber}");

                    break;
                case WorkflowStage.Rejected:
                    // Reject from Review to Assess
                    _logger.LogInformation("Starting rejecting k2 task with ProcessId {ProcessId} and SerialNumber {SerialNumber} from {FromActivity}");

                    success = await _workflowServiceApiClient.RejectWorkflowInstance(k2Task.SerialNumber);

                    if (!success)
                    {
                        _logger.LogError("Unable to reject task {ProcessId} from {FromActivity} in K2.");
                        throw new ApplicationException($"Unable to reject task {message.ProcessId} from {message.FromActivity} in K2.");
                    }

                    _logger.LogInformation("Successfully completed the request to reject k2 task with ProcessId {ProcessId} and SerialNumber {SerialNumber} from {message.FromActivity}");


                    break;
                case WorkflowStage.Assess:
                    // Progressing task from Review to Assess

                    _logger.LogInformation("Starting progressing k2 task with ProcessId {ProcessId} and SerialNumber {SerialNumber} from {FromActivity} to {ToActivity}");
                    
                    success = await _workflowServiceApiClient.ProgressWorkflowInstance(k2Task.SerialNumber);
                    
                    if (!success)
                    {
                        _logger.LogError("Unable to progress task {ProcessId} from {FromActivity} to {ToActivity} in K2.");
                        throw new ApplicationException($"Unable to progress task {message.ProcessId} from {message.FromActivity} to {message.ToActivity} in K2.");
                    }

                    _logger.LogInformation("Successfully completed the request to progress k2 task with ProcessId {ProcessId} and SerialNumber {SerialNumber} from {message.FromActivity} to {message.ToActivity}");

                    break;
                case WorkflowStage.Verify:
                    // Progressing task from Assess to Verify

                    _logger.LogInformation("Starting progressing k2 task with ProcessId {ProcessId} and SerialNumber {SerialNumber} from {FromActivity} to {ToActivity}");

                    success = await _workflowServiceApiClient.ProgressWorkflowInstance(k2Task.SerialNumber);

                    if (!success)
                    {
                        _logger.LogError("Unable to progress task {ProcessId} from {FromActivity} to {ToActivity} in K2.");
                        throw new ApplicationException($"Unable to progress task {message.ProcessId} from {message.FromActivity} to {message.ToActivity} in K2.");
                    }

                    _logger.LogInformation("Successfully completed the request to progress k2 task with ProcessId {ProcessId} and SerialNumber {SerialNumber} from {message.FromActivity} to {message.ToActivity}");

                    break;
                case WorkflowStage.Completed:
                    // Progressing task from Verify to Completed

                    _logger.LogInformation("Starting progressing k2 task with ProcessId {ProcessId} and SerialNumber {SerialNumber} from {FromActivity} to {ToActivity}");

                    success = await _workflowServiceApiClient.ProgressWorkflowInstance(k2Task.SerialNumber);

                    if (!success)
                    {
                        _logger.LogError("Unable to progress task {ProcessId} from {FromActivity} to {ToActivity} in K2.");
                        throw new ApplicationException($"Unable to progress task {message.ProcessId} from {message.FromActivity} to {message.ToActivity} in K2.");
                    }

                    _logger.LogInformation("Successfully completed the request to progress k2 task with ProcessId {ProcessId} and SerialNumber {SerialNumber} from {message.FromActivity} to {message.ToActivity}");

                    break;
                default:
                    throw new NotImplementedException($"{message.ToActivity} has not been implemented for processId: {message.ProcessId}.");
            }

            // fire persist command
            var persistWorkflowInstanceDataCommand = new PersistWorkflowInstanceDataCommand()
            {
                CorrelationId = message.CorrelationId,
                ProcessId = message.ProcessId,
                FromActivity = message.FromActivity,
                ToActivity = message.ToActivity
            };

            await context.SendLocal(persistWorkflowInstanceDataCommand).ConfigureAwait(false);
            _logger.LogInformation("Successfully Completed Event {EventName}: {Message}");
        }

        private void ValidateK2TaskPriorToProgressing(ProgressWorkflowInstanceEvent message, K2TaskData k2Task)
        {
            if (k2Task == null)
            {
                _logger.LogError("Failed to get data for K2 Task with ProcessId {ProcessId} prior to progressing task from {FromActivity} to {ToActivity}");
                throw new ApplicationException($"Failed to get data for K2 Task with ProcessId {message.ProcessId} prior to moving task from {message.FromActivity} to {message.ToActivity}");
            }

            if (k2Task.ActivityName != message.FromActivity.ToString())
            {
                LogContext.PushProperty("K2Stage", k2Task.ActivityName);
                _logger.LogError(
                        "Workflow instance with ProcessId {ProcessId} is not at the expected step {FromActivity} in K2 but was at {K2Stage}," +
                        " while progressing task to {ToActivity}");
                throw new ApplicationException(
                        $"Workflow instance with ProcessId {message.ProcessId} is not at the expected step {message.FromActivity} in K2 but was at {k2Task.ActivityName}," +
                        $" while progressing task to {message.ToActivity}");
            }
        }

        private async Task UpdateK2WorkflowAsTerminated(string k2SerialNumber)
        {
            try
            {
                await _workflowServiceApiClient.TerminateWorkflowInstance(k2SerialNumber);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed Terminating K2 task with ProcessId {ProcessId} and SerialNumber: {SerialNumber}");

                throw new ApplicationException(
                    $"Failed Terminating K2 task with SerialNumber: {k2SerialNumber}{Environment.NewLine}{e.Message}");
            }
        }

    }
}
