using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Commands;
using Common.Messages.Events;
using Microsoft.EntityFrameworkCore;
using NServiceBus;
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

        public StartWorkflowInstanceEventHandler(IWorkflowServiceApiClient workflowServiceApiClient, WorkflowDbContext dbContext)
        {
            _workflowServiceApiClient = workflowServiceApiClient;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Create a new Db Assessment workflow instance and progress it to the Assess step.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Handle(StartWorkflowInstanceEvent message, IMessageHandlerContext context)
        {
            var dbAssessmentWorkflowId = await _workflowServiceApiClient.GetDBAssessmentWorkflowId();

            // We get the Process Id back...
            var processId = await _workflowServiceApiClient.CreateWorkflowInstance(dbAssessmentWorkflowId);

            // Get the instance serial no
            var serialNumber = await _workflowServiceApiClient.GetWorkflowInstanceSerialNumber(processId);

            if (string.IsNullOrEmpty(serialNumber))
            {
                // TODO: Log
                // _logger.LogError("Failed to get data for K2 Task with ProcessId {ProcessId}");
                throw new ApplicationException($"Failed to get data for K2 Task with ProcessId {processId}");
            }

            // Progress this new instance onto Assess
            await _workflowServiceApiClient.ProgressWorkflowInstance(processId, serialNumber);

            var persistChildData = new PersistChildWorkflowDataCommand
            {
                AssignedTaskId = message.AssignedTaskId,
                CorrelationId = message.CorrelationId,
                ParentProcessId = message.ParentProcessId,
                ChildProcessId = processId
            };

            await context.SendLocal(persistChildData).ConfigureAwait(false);
        }

        /// <summary>
        /// Persist the relevant information about the new child task in the db
        /// </summary>
        /// <param name="command"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Handle(PersistChildWorkflowDataCommand command, IMessageHandlerContext context)
        {
            // Get the parent's data... 
            var reviewData =
                await _dbContext.DbAssessmentReviewData.FirstAsync(d => d.ProcessId == command.ParentProcessId);
            var additionalAssignedTaskData =
                await _dbContext.DbAssessmentAssignTask.FirstAsync(d => d.DbAssessmentAssignTaskId == command.AssignedTaskId);
            var parentData = await _dbContext.AssessmentData.FirstAsync(d => d.ProcessId == command.ParentProcessId);
            var primarySourceData = await _dbContext.PrimaryDocumentStatus.FirstAsync(d => d.ProcessId == command.ParentProcessId);

            var newSn = await _workflowServiceApiClient.GetWorkflowInstanceSerialNumber(command.ChildProcessId);

            // ...and create for the new child
            WorkflowInstance newWorkflowInstance = null;

            if (!_dbContext.WorkflowInstance.Any(w => w.ProcessId == command.ChildProcessId))
            {
                newWorkflowInstance = new WorkflowInstance
                {
                    ProcessId = command.ChildProcessId,
                    Comments = new List<Comment>(),
                    ActivityName = "Assess",
                    ParentProcessId = command.ParentProcessId,
                    SerialNumber = newSn,
                    StartedAt = DateTime.Today,
                    Status = WorkflowStatus.Started.ToString()
                };

                await _dbContext.WorkflowInstance.AddAsync(newWorkflowInstance);

            }

            await PersistAssessmentDataFromParent(command.ChildProcessId, parentData);
            await PersistPrimaryDocumentFromParent(
                                                    command.ChildProcessId,
                                                    command.CorrelationId,
                                                    parentData.PrimarySdocId,
                                                    primarySourceData);

            await _dbContext.SaveChangesAsync();

            await PersistChildWorkflowDataToAssess(
                                                    command.ChildProcessId,
                                                    newWorkflowInstance.WorkflowInstanceId,
                                                    reviewData, additionalAssignedTaskData);

            await CopyAdditionalAssignTaskNoteToComments(
                                                            command.ParentProcessId, 
                                                            command.ChildProcessId,
                                                            additionalAssignedTaskData.Notes,
                                                            newWorkflowInstance.WorkflowInstanceId,
                                                            reviewData.Reviewer);

            await _dbContext.SaveChangesAsync();

        }

        private async Task CopyAdditionalAssignTaskNoteToComments(
                                                                    int parentProcessId,
                                                                    int childProcessId,
                                                                    string assignTaskNote,
                                                                    int newWorkflowInstance,
                                                                    string reviewer)
        {
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
        }

        private async Task PersistChildWorkflowDataToAssess(
                                                        int childProcessId,
                                                        int  newWorkflowInstanceId,
                                                        DbAssessmentReviewData reviewData,
                                                        DbAssessmentAssignTask additionalAssignedTaskData)
        {
            if (! await _dbContext.DbAssessmentAssessData.AnyAsync(d => d.ProcessId == childProcessId))
            {
                await _dbContext.DbAssessmentAssessData.AddAsync(new DbAssessmentAssessData
                {
                    ProcessId = childProcessId,
                    WorkflowInstanceId = newWorkflowInstanceId,

                    ActivityCode = reviewData.ActivityCode,
                    Ion = reviewData.Ion,
                    SourceCategory = reviewData.SourceCategory,
                    WorkspaceAffected = additionalAssignedTaskData.WorkspaceAffected,
                    TaskType = additionalAssignedTaskData.TaskType,
                    Reviewer = reviewData.Reviewer,
                    Assessor = additionalAssignedTaskData.Assessor,
                    Verifier = additionalAssignedTaskData.Verifier
                });
            }
        }

        private async Task PersistPrimaryDocumentFromParent(
                                                        int childProcessId,
                                                        Guid correlationId,
                                                        int primarySdocId,
            PrimaryDocumentStatus primarySourceData)
        {
            if (!await _dbContext.PrimaryDocumentStatus.AnyAsync(p => p.ProcessId == childProcessId))
            {
                await _dbContext.PrimaryDocumentStatus.AddAsync(new PrimaryDocumentStatus
                {
                    ProcessId = childProcessId,
                    CorrelationId = correlationId,
                    SdocId = primarySdocId,
                    Status = primarySourceData.Status,
                    StartedAt = primarySourceData.StartedAt,
                    ContentServiceId = primarySourceData.ContentServiceId
                });
            }
        }

        private async Task PersistAssessmentDataFromParent(int childProcessId, AssessmentData parentData)
        {
            if (!await _dbContext.AssessmentData.AnyAsync(a => a.ProcessId == childProcessId))
            {
                await _dbContext.AssessmentData.AddAsync(new AssessmentData
                {
                    Datum = parentData.Datum,
                    EffectiveStartDate = parentData.EffectiveStartDate,
                    PrimarySdocId = parentData.PrimarySdocId,
                    ReceiptDate = parentData.ReceiptDate,
                    RsdraNumber = parentData.RsdraNumber,
                    ProcessId = childProcessId,
                    SourceDocumentName = parentData.SourceDocumentName,
                    SourceDocumentType = parentData.SourceDocumentType,
                    TeamDistributedTo = parentData.TeamDistributedTo,
                    SourceNature = parentData.SourceNature,
                    ToSdoDate = parentData.ToSdoDate
                });
            }
        }
    }
}
