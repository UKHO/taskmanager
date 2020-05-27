using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Commands;
using Common.Messages.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using WorkflowCoordinator.HttpClients;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.Handlers
{
    public class StartWorkflowInstanceEventHandler : IHandleMessages<StartWorkflowInstanceEvent>,
        IHandleMessages<PersistChildWorkflowDataCommand>
    {
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly WorkflowDbContext _dbContext;
        private readonly ILogger<StartWorkflowInstanceEventHandler> _logger;

        public StartWorkflowInstanceEventHandler(IWorkflowServiceApiClient workflowServiceApiClient, WorkflowDbContext dbContext, ILogger<StartWorkflowInstanceEventHandler> logger)
        {
            _workflowServiceApiClient = workflowServiceApiClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Create a new Db Assessment workflow instance and progress it to the Assess step.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Handle(StartWorkflowInstanceEvent message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(StartWorkflowInstanceEvent));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ParentProcessId", message.ParentProcessId);
            LogContext.PushProperty("WorkflowType", message.WorkflowType);
            LogContext.PushProperty("AssignedTaskId", message.AssignedTaskId);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            var dbAssessmentWorkflowId = await _workflowServiceApiClient.GetDBAssessmentWorkflowId();

            // We get the Process Id back...
            var processId = await _workflowServiceApiClient.CreateWorkflowInstance(dbAssessmentWorkflowId);
            
            LogContext.PushProperty("ProcessId", processId);

            // Get the instance serial no
            var serialNumber = await _workflowServiceApiClient.GetWorkflowInstanceSerialNumber(processId);

            if (string.IsNullOrEmpty(serialNumber))
            {
                _logger.LogError("Failed to get K2 Task serial number for ProcessId {ProcessId}");
                throw new ApplicationException($"Failed to get K2 Task serial number for ProcessId {processId}");
            }
            
            LogContext.PushProperty("SerialNumber", serialNumber);

            _logger.LogInformation("Successfully created K2 task with ProcessId: {ProcessId} and SerialNumber: {SerialNumber} as a Child workflow to ParentProcessId: {ParentProcessId}");
            
            // Progress this new instance onto Assess
            await _workflowServiceApiClient.ProgressWorkflowInstance(serialNumber);

            var persistChildData = new PersistChildWorkflowDataCommand
            {
                AssignedTaskId = message.AssignedTaskId,
                CorrelationId = message.CorrelationId,
                ParentProcessId = message.ParentProcessId,
                ChildProcessId = processId
            };

            await context.SendLocal(persistChildData).ConfigureAwait(false);

            _logger.LogInformation("Successfully Completed Event {EventName}: {Message}");

        }

        /// <summary>
        /// Persist the relevant information about the new child task in the db
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Handle(PersistChildWorkflowDataCommand message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(PersistChildWorkflowDataCommand));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ParentProcessId", message.ParentProcessId);
            LogContext.PushProperty("ProcessId", message.ChildProcessId);
            LogContext.PushProperty("AssignedTaskId", message.AssignedTaskId);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");
            
            // Get the parent's data... 
            var parentWorkflowInstanceData =
                await _dbContext.WorkflowInstance
                    .Include(w => w.DbAssessmentReviewData)
                    .Include(w => w.AssessmentData)
                    .Include(w => w.PrimaryDocumentStatus)
                    .FirstAsync(w => w.ProcessId == message.ParentProcessId);

            if (parentWorkflowInstanceData == null)
            {
                _logger.LogError("Failed to get parent data from WorkflowInstance with ParentProcessId {ParentProcessId} for ChildProcessId {ProcessId}");
                throw new ApplicationException($"Failed to get parent data from WorkflowInstance with ParentProcessId {message.ParentProcessId} for ChildProcessId {message.ChildProcessId}");
            }
            
            var additionalAssignedTaskData =
                await _dbContext.DbAssessmentAssignTask.FirstAsync(d => d.DbAssessmentAssignTaskId == message.AssignedTaskId);

            var newSn = await _workflowServiceApiClient.GetWorkflowInstanceSerialNumber(message.ChildProcessId);

            if (string.IsNullOrEmpty(newSn))
            {
                _logger.LogError("Failed to get K2 Task serial number for ProcessId {ProcessId}");
                throw new ApplicationException($"Failed to get K2 Task serial number for ProcessId {message.ChildProcessId}");
            }
            
            // ...and create for the new child

            var newWorkflowInstance = await PersistChildWorkflowInstance(message, parentWorkflowInstanceData, newSn);

            await PersistAssessmentDataFromParent(message.ChildProcessId, parentWorkflowInstanceData.AssessmentData);
            await PersistPrimaryDocumentFromParent(
                                                    message.ChildProcessId,
                                                    message.CorrelationId,
                                                    parentWorkflowInstanceData.AssessmentData.PrimarySdocId,
                                                    parentWorkflowInstanceData.PrimaryDocumentStatus);

            await _dbContext.SaveChangesAsync();

            await PersistChildWorkflowDataToAssess(
                                                    message.ChildProcessId,
                                                    newWorkflowInstance.WorkflowInstanceId,
                                                    parentWorkflowInstanceData.DbAssessmentReviewData, additionalAssignedTaskData);

            await CopyAdditionalAssignTaskNoteToComments(
                                                            message.ParentProcessId,
                                                            message.ChildProcessId,
                                                            additionalAssignedTaskData.Notes,
                                                            newWorkflowInstance.WorkflowInstanceId,
                                                            parentWorkflowInstanceData.DbAssessmentReviewData.Reviewer);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully Completed {EventName}: {Message}");

        }

        private async Task<WorkflowInstance> PersistChildWorkflowInstance(PersistChildWorkflowDataCommand command,
            WorkflowInstance parentWorkflowInstanceData, string newSn)
        {
            _logger.LogInformation("Entering PersistChildWorkflowInstance method with ParentProcessId {ParentProcessId} and ChildProcessId {ProcessId}");

            var childWorkflowInstance =
                await _dbContext.WorkflowInstance.FirstOrDefaultAsync(w => w.ProcessId == command.ChildProcessId);

            var isNew = childWorkflowInstance == null;

            if (isNew)
            {
                childWorkflowInstance = new WorkflowInstance();
            }

            childWorkflowInstance.ProcessId = command.ChildProcessId;
            childWorkflowInstance.PrimarySdocId = parentWorkflowInstanceData.PrimarySdocId;
            childWorkflowInstance.Comments = new List<Comment>();
            childWorkflowInstance.ActivityName = WorkflowStage.Assess.ToString();
            childWorkflowInstance.ParentProcessId = command.ParentProcessId;
            childWorkflowInstance.SerialNumber = newSn;
            childWorkflowInstance.StartedAt = DateTime.Today;
            childWorkflowInstance.Status = WorkflowStatus.Started.ToString();
            childWorkflowInstance.ActivityChangedAt = DateTime.Today;

            if (isNew)
            {
                await _dbContext.WorkflowInstance.AddAsync(childWorkflowInstance);
            }

            _logger.LogInformation("Successfully updated child WorkflowInstance table with ParentProcessId {ParentProcessId} and ChildProcessId {ProcessId}");

            return childWorkflowInstance;
        }

        private async Task CopyAdditionalAssignTaskNoteToComments(
                                                                    int parentProcessId,
                                                                    int childProcessId,
                                                                    string assignTaskNote,
                                                                    int newWorkflowInstance,
                                                                    string reviewer)
        {
            _logger.LogInformation("Entering CopyAdditionalAssignTaskNoteToComments method with ParentProcessId {ParentProcessId} and ChildProcessId {ProcessId}");

            if (!string.IsNullOrEmpty(assignTaskNote))
            {
                if (!await _dbContext.Comment.AnyAsync(c =>
                                                                        c.ProcessId == childProcessId
                                                                        && c.Text.StartsWith("Assign Task:")))
                {
                    await _dbContext.Comment.AddAsync(new Comment()
                    {
                        ProcessId = childProcessId,
                        WorkflowInstanceId = newWorkflowInstance,
                        Text = $"Assign Task (Parent processId: {parentProcessId}): {assignTaskNote.Trim()}",
                        Username = reviewer,
                        Created = DateTime.Today
                    });
                }
            }

            _logger.LogInformation("Successfully updated child Comment table with ParentProcessId {ParentProcessId} and ChildProcessId {ProcessId}");

        }

        private async Task PersistChildWorkflowDataToAssess(
                                                        int childProcessId,
                                                        int newWorkflowInstanceId,
                                                        DbAssessmentReviewData parentReviewData,
                                                        DbAssessmentAssignTask additionalAssignedTaskData)
        {
            _logger.LogInformation("Entering PersistChildWorkflowDataToAssess method with ParentProcessId {ParentProcessId} and ChildProcessId {ProcessId}");

            if (parentReviewData == null)
            {
                _logger.LogError("Failed to get parent data from DbAssessmentReviewData with ParentProcessId {ParentProcessId} for ChildProcessId {ProcessId}");
                throw new ApplicationException($"Failed to get parent data from DbAssessmentReviewData for ChildProcessId {childProcessId}");
            }

            if (additionalAssignedTaskData == null)
            {
                _logger.LogError("Failed to get parent data from DbAssessmentAssignTask with ParentProcessId {ParentProcessId} for ChildProcessId {ProcessId}");
                throw new ApplicationException($"Failed to get parent data from DbAssessmentAssignTask for ChildProcessId {childProcessId}");
            }
            
            var childAssessData =
                await _dbContext.DbAssessmentAssessData.FirstOrDefaultAsync(a => a.ProcessId == childProcessId);

            var isNew = childAssessData == null;

            if (isNew)
            {
                childAssessData = new DbAssessmentAssessData();
            }

            childAssessData.ProcessId = childProcessId;
            childAssessData.WorkflowInstanceId = newWorkflowInstanceId;

            childAssessData.ActivityCode = parentReviewData.ActivityCode;
            childAssessData.Ion = parentReviewData.Ion;
            childAssessData.SourceCategory = parentReviewData.SourceCategory;
            childAssessData.WorkspaceAffected = additionalAssignedTaskData.WorkspaceAffected;
            childAssessData.TaskType = additionalAssignedTaskData.TaskType;
            childAssessData.Reviewer = parentReviewData.Reviewer;
            childAssessData.Assessor = additionalAssignedTaskData.Assessor;
            childAssessData.Verifier = additionalAssignedTaskData.Verifier;

            if (isNew)
            {
                await _dbContext.DbAssessmentAssessData.AddAsync(childAssessData);
            }

            _logger.LogInformation("Successfully updated child DbAssessmentAssessData table with ParentProcessId {ParentProcessId} and ChildProcessId {ProcessId}");

        }

        private async Task PersistPrimaryDocumentFromParent(
                                                        int childProcessId,
                                                        Guid correlationId,
                                                        int primarySdocId,
                                                        PrimaryDocumentStatus parentPrimarySourceData)
        {
            _logger.LogInformation("Entering PersistPrimaryDocumentFromParent method with ParentProcessId {ParentProcessId} and ChildProcessId {ProcessId}");

            if (parentPrimarySourceData == null)
            {
                _logger.LogError("Failed to get parent data from PrimaryDocumentStatus with ParentProcessId {ParentProcessId} for ChildProcessId {ProcessId}");
                throw new ApplicationException($"Failed to get parent data from PrimaryDocumentStatus for ChildProcessId {childProcessId}");
            }

            var childPrimaryDocumentStatus =
                await _dbContext.PrimaryDocumentStatus.FirstOrDefaultAsync(p => p.ProcessId == childProcessId);

            var isNew = childPrimaryDocumentStatus == null;

            if (isNew)
            {
                childPrimaryDocumentStatus = new PrimaryDocumentStatus();
            }

            childPrimaryDocumentStatus.ProcessId = childProcessId;
            childPrimaryDocumentStatus.CorrelationId = correlationId;
            childPrimaryDocumentStatus.SdocId = primarySdocId;
            childPrimaryDocumentStatus.Status = parentPrimarySourceData.Status;
            childPrimaryDocumentStatus.StartedAt = parentPrimarySourceData.StartedAt;
            childPrimaryDocumentStatus.ContentServiceId = parentPrimarySourceData.ContentServiceId;

            if (isNew)
            {
                await _dbContext.PrimaryDocumentStatus.AddAsync(childPrimaryDocumentStatus);
            }

            _logger.LogInformation("Successfully updated child PrimaryDocumentStatus table with ParentProcessId {ParentProcessId} and ChildProcessId {ProcessId}");

        }

        private async Task PersistAssessmentDataFromParent(int childProcessId, AssessmentData parentAssessmentData)
        {
            _logger.LogInformation("Entering PersistAssessmentDataFromParent method with ParentProcessId {ParentProcessId} and ChildProcessId {ProcessId}");

            if (parentAssessmentData == null)
            {
                _logger.LogError("Failed to get parent data from AssessmentData with ParentProcessId {ParentProcessId} for ChildProcessId {ProcessId}");
                throw new ApplicationException($"Failed to get parent data from AssessmentData for ChildProcessId {childProcessId}");
            }

            var childAssessmentData =
                await _dbContext.AssessmentData.FirstOrDefaultAsync(a => a.ProcessId == childProcessId);

            var isNew = childAssessmentData == null;

            if (isNew)
            {
                childAssessmentData = new AssessmentData();
            }

            childAssessmentData.Datum = parentAssessmentData.Datum;
            childAssessmentData.EffectiveStartDate = parentAssessmentData.EffectiveStartDate;
            childAssessmentData.PrimarySdocId = parentAssessmentData.PrimarySdocId;
            childAssessmentData.ReceiptDate = parentAssessmentData.ReceiptDate;
            childAssessmentData.RsdraNumber = parentAssessmentData.RsdraNumber;
            childAssessmentData.ProcessId = childProcessId;
            childAssessmentData.SourceDocumentName = parentAssessmentData.SourceDocumentName;
            childAssessmentData.SourceDocumentType = parentAssessmentData.SourceDocumentType;
            childAssessmentData.TeamDistributedTo = parentAssessmentData.TeamDistributedTo;
            childAssessmentData.SourceNature = parentAssessmentData.SourceNature;
            childAssessmentData.ToSdoDate = parentAssessmentData.ToSdoDate;

            if (isNew)
            {
                await _dbContext.AssessmentData.AddAsync(childAssessmentData);
            }

            _logger.LogInformation("Successfully updated child AssessmentData table with ParentProcessId {ParentProcessId} and ChildProcessId {ProcessId}");

        }
    }
}
