using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Enums;
using Common.Messages.Events;
using NServiceBus;
using WorkflowCoordinator.HttpClients;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.Handlers
{
    public class StartWorkflowInstanceEventHandler : IHandleMessages<StartWorkflowInstanceEvent>
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

            // Get the parent's assessment data...
            var parentData = _dbContext.AssessmentData.First(d => d.ProcessId == message.ParentProcessId);

            // ...and create for the new child
            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = instanceId,
                Comments = new List<Comment>(),
                ActivityName = "Assess", // It isn't actually at Assess yet...
                ParentProcessId = message.ParentProcessId,
                SerialNumber = sn,
                StartedAt = DateTime.Today,
                Status = WorkflowStatus.Started.ToString(),
                WorkflowType = WorkflowType.DbAssessment.ToString()
            });

            _dbContext.AssessmentData.Add(new AssessmentData
            {
                Datum = parentData.Datum,
                EffectiveStartDate = parentData.EffectiveStartDate,
                PrimarySdocId = parentData.PrimarySdocId,
                ReceiptDate = parentData.ReceiptDate,
                RsdraNumber = parentData.RsdraNumber,
                ProcessId = instanceId,
                SourceDocumentName = parentData.SourceDocumentName,
                SourceDocumentType = parentData.SourceDocumentType,
                TeamDistributedTo = parentData.TeamDistributedTo,
                SourceNature = parentData.SourceNature,
                ToSdoDate = parentData.ToSdoDate
            });

            await _dbContext.SaveChangesAsync();

            // Progress this new instance onto Assess
            await _workflowServiceApiClient.ProgressWorkflowInstance(sn, "Assess");


        }
    }
}
