using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Commands;
using Common.Messages.Enums;
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
            var instanceId = await _workflowServiceApiClient.CreateWorkflowInstance(dbAssessmentWorkflowId);

            // Get the instance serial no
            var sn = await _workflowServiceApiClient.GetWorkflowInstanceSerialNumber(instanceId);

            // Progress this new instance onto Assess
            await _workflowServiceApiClient.ProgressWorkflowInstance(instanceId, sn);

            var newSn = await _workflowServiceApiClient.GetWorkflowInstanceSerialNumber(instanceId);

            var persistChildData = new PersistChildWorkflowDataCommand
            {
                AssignedTaskId = message.AssignedTaskId,
                CorrelationId = message.CorrelationId,
                ParentProcessId = message.ParentProcessId,
                ChildProcessId = instanceId,
                ChildProcessSerialNumber = newSn
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

            // ...and create for the new child
            WorkflowInstance newWorkflowInstance = null;

            if (!_dbContext.WorkflowInstance.Any(w => w.ProcessId == command.ChildProcessId))
            {
                newWorkflowInstance = new WorkflowInstance
                {
                    ProcessId = command.ChildProcessId,
                    Comments = new List<Comment>(),
                    ActivityName = "Assess", // It isn't actually at Assess yet...
                    ParentProcessId = command.ParentProcessId,
                    SerialNumber = command.ChildProcessSerialNumber,
                    StartedAt = DateTime.Today,
                    Status = WorkflowStatus.Started.ToString(),
                    WorkflowType = WorkflowType.DbAssessment.ToString()
                };

                _dbContext.WorkflowInstance.Add(newWorkflowInstance);

            }

            if (!_dbContext.AssessmentData.Any(a => a.ProcessId == command.ChildProcessId))
            {

                _dbContext.AssessmentData.Add(new AssessmentData
                {
                    Datum = parentData.Datum,
                    EffectiveStartDate = parentData.EffectiveStartDate,
                    PrimarySdocId = parentData.PrimarySdocId,
                    ReceiptDate = parentData.ReceiptDate,
                    RsdraNumber = parentData.RsdraNumber,
                    ProcessId = command.ChildProcessId,
                    SourceDocumentName = parentData.SourceDocumentName,
                    SourceDocumentType = parentData.SourceDocumentType,
                    TeamDistributedTo = parentData.TeamDistributedTo,
                    SourceNature = parentData.SourceNature,
                    ToSdoDate = parentData.ToSdoDate
                });

            }

            if (!_dbContext.PrimaryDocumentStatus.Any(p => p.ProcessId == command.ChildProcessId))
            {

                _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus
                {
                    ProcessId = command.ChildProcessId,
                    CorrelationId = command.CorrelationId,
                    SdocId = parentData.PrimarySdocId,
                    Status = primarySourceData.Status,
                    StartedAt = primarySourceData.StartedAt,
                    ContentServiceId = primarySourceData.ContentServiceId
                });

            }

            await _dbContext.SaveChangesAsync();

            if (!_dbContext.DbAssessmentAssessData.Any(d => d.ProcessId == command.ChildProcessId))
            {
                _dbContext.DbAssessmentAssessData.Add(new DbAssessmentAssessData
                {
                    ProcessId = command.ChildProcessId,
                    WorkflowInstanceId = newWorkflowInstance.WorkflowInstanceId,

                    ActivityCode = reviewData.ActivityCode,
                    Ion = reviewData.Ion,
                    SourceCategory = reviewData.SourceCategory,
                    Reviewer = reviewData.Reviewer,

                    Assessor = additionalAssignedTaskData.Assessor,
                    Verifier = additionalAssignedTaskData.Verifier,
                    TaskType = additionalAssignedTaskData.TaskType
                });


                await _dbContext.SaveChangesAsync();

                // TODO: Copy additionalAssignedTaskData Notes to Comments; See 'CopyPrimaryAssignTaskNoteToComments' in Review.cshtml.cs
            }
        }
    }
}
