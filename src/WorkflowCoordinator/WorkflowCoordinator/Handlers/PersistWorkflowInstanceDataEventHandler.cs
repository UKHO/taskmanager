﻿using System;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Models;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

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
            LogContext.PushProperty("FromActivity", message.FromActivity);
            LogContext.PushProperty("ToActivity", message.ToActivity);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            var k2Task = await _workflowServiceApiClient.GetWorkflowInstanceData(message.ProcessId);

            WorkflowInstance workflowInstance = null;

            switch (message.ToActivity)
            {
                case WorkflowStage.Assess:

                    ValidateK2TaskForAssessAndVerify(message, k2Task);

                    workflowInstance = await UpdateWorkflowInstanceData(message.ProcessId, message.ToActivity, k2Task);

                    var isRejected = message.FromActivity == WorkflowStage.Verify;

                    if (isRejected)
                    {
                        await PersistWorkflowDataToAssessFromVerify(message.ProcessId, message.FromActivity, workflowInstance);
                    }
                    else
                    {
                        await PersistWorkflowDataToAssessFromReview(message.ProcessId, message.FromActivity, workflowInstance);
                    }

                    break;
                case WorkflowStage.Verify:

                    ValidateK2TaskForAssessAndVerify(message, k2Task);

                    workflowInstance = await UpdateWorkflowInstanceData(message.ProcessId, message.ToActivity, k2Task);

                    await PersistWorkflowDataToVerifyFromAssess(message.ProcessId, workflowInstance.WorkflowInstanceId);

                    break;
                case WorkflowStage.Completed:

                    ValidateK2TaskForSignOff(message, k2Task);

                    await UpdateWorkflowInstanceData(message.ProcessId, message.ToActivity, k2Task);

                    _logger.LogInformation("Task with processId: {ProcessId} has been completed.");

                    break;
                default:
                    throw new NotImplementedException($"{message.ToActivity} has not been implemented for processId: {message.ProcessId}.");
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully Completed Event {EventName}: {Message}");

        }

        private void ValidateK2TaskForSignOff(PersistWorkflowInstanceDataEvent message, K2TaskData k2Task)
        {
            if (k2Task != null)
            {
                // if task as at Completed, then we expect k2Task to be null
                _logger.LogError("K2 Task is not at expected stage {ToActivity} for ProcessId {ProcessId}; but was at " +
                                 k2Task.ActivityName);
                throw new ApplicationException(
                    $"K2 Task is not at expected stage {message.ToActivity} for ProcessId {message.ProcessId}; but was at {k2Task.ActivityName}");
            }
        }

        private void ValidateK2TaskForAssessAndVerify(PersistWorkflowInstanceDataEvent message, K2TaskData k2Task)
        {
            if (k2Task == null)
            {
                _logger.LogError("Failed to get data for K2 Task at stage with ProcessId {ProcessId}");
                throw new ApplicationException($"Failed to get data for K2 Task at stage with ProcessId {message.ProcessId}");
            }

            if (k2Task.ActivityName != message.ToActivity.ToString())
            {
                LogContext.PushProperty("K2Stage", k2Task.ActivityName);
                _logger.LogError("K2Task at stage {K2Stage} is not at {ToActivity}");
                throw new ApplicationException($"K2Task at stage {k2Task.ActivityName} is not at {message.ToActivity}");
            }
        }

        private async Task<WorkflowInstance> UpdateWorkflowInstanceData(int processId, WorkflowStage toActivity, K2TaskData k2Task)
        {
            var workflowInstance = await _dbContext.WorkflowInstance.Include(wi => wi.ProductAction)
                .Include(wi => wi.DataImpact)
                .FirstAsync(wi => wi.ProcessId == processId);

            workflowInstance.SerialNumber = (toActivity == WorkflowStage.Completed) ? "" : k2Task.SerialNumber;
            workflowInstance.ActivityName = (toActivity == WorkflowStage.Completed) ? WorkflowStage.Completed.ToString() : k2Task.ActivityName;

            workflowInstance.Status = (toActivity == WorkflowStage.Completed) ? WorkflowStage.Completed.ToString() : WorkflowStatus.Started.ToString();

            return workflowInstance;

        }

        private async Task PersistWorkflowDataToAssessFromReview(int processId, WorkflowStage fromActivity, WorkflowInstance workflowInstance)
        {
            LogContext.PushProperty("PersistWorkflowDataToAssessFromReview", nameof(PersistWorkflowDataToAssessFromReview));
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("WorkflowInstanceId", workflowInstance.WorkflowInstanceId);
            LogContext.PushProperty("FromActivity", fromActivity);

            _logger.LogInformation("Entering {PersistWorkflowDataToAssessFromReview} with processId: {ProcessId}; " +
                                   "workflowInstanceId: {WorkflowInstanceId}; and FromActivity: {FromActivity}.");


            var reviewData = await _dbContext.DbAssessmentReviewData.SingleAsync(d => d.ProcessId == processId);

            _logger.LogInformation("Saving primary task data from review to assess for processId: {ProcessId} and workflowInstanceId: {WorkflowInstanceId}.");

            var assessData =
                await _dbContext.DbAssessmentAssessData.SingleOrDefaultAsync(d => d.ProcessId == processId);

            var isExists = (assessData != null);

            if (!isExists)
            {
                assessData = new DbAssessmentAssessData();
            }

            assessData.ProcessId = processId;
            assessData.WorkflowInstanceId = workflowInstance.WorkflowInstanceId;

            assessData.ActivityCode = reviewData.ActivityCode;
            assessData.Ion = reviewData.Ion;
            assessData.SourceCategory = reviewData.SourceCategory;
            assessData.WorkspaceAffected = reviewData.WorkspaceAffected;
            assessData.TaskType = reviewData.TaskType;
            assessData.Reviewer = reviewData.Reviewer;
            assessData.Assessor = reviewData.Assessor;
            assessData.Verifier = reviewData.Verifier;

            if (!isExists)
            {
                await _dbContext.DbAssessmentAssessData.AddAsync(assessData);
            }
        }

        private async Task PersistWorkflowDataToAssessFromVerify(int processId, WorkflowStage fromActivity, WorkflowInstance workflowInstance)
        {
            LogContext.PushProperty("PersistWorkflowDataToAssessFromVerify", nameof(PersistWorkflowDataToAssessFromVerify));
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("WorkflowInstanceId", workflowInstance.WorkflowInstanceId);
            LogContext.PushProperty("FromActivity", fromActivity);

            _logger.LogInformation("Entering {PersistWorkflowDataToAssessFromVerify} with processId: {ProcessId}; " +
                                   "workflowInstanceId: {WorkflowInstanceId}; and FromActivity: {FromActivity}.");


            foreach (var productAction in workflowInstance.ProductAction)
            {
                productAction.Verified = false;
            }

            foreach (var dataImpact in workflowInstance.DataImpact)
            {
                dataImpact.Verified = false;
            }

            var verifyData = await _dbContext.DbAssessmentVerifyData.SingleAsync(d => d.ProcessId == processId);

            _logger.LogInformation("Saving primary task data from verify to assess for processId: {ProcessId} and workflowInstanceId: {WorkflowInstanceId}.");

            var assessData =
                await _dbContext.DbAssessmentAssessData.SingleOrDefaultAsync(d => d.ProcessId == processId);

            var isExists = (assessData != null);

            if (!isExists)
            {
                assessData = new DbAssessmentAssessData();
            }

            assessData.ProcessId = processId;
            assessData.WorkflowInstanceId = workflowInstance.WorkflowInstanceId;

            assessData.ActivityCode = verifyData.ActivityCode;
            assessData.Ion = verifyData.Ion;
            assessData.SourceCategory = verifyData.SourceCategory;
            assessData.WorkspaceAffected = verifyData.WorkspaceAffected;
            assessData.TaskType = verifyData.TaskType;
            assessData.Reviewer = verifyData.Reviewer;
            assessData.Assessor = verifyData.Assessor;
            assessData.Verifier = verifyData.Verifier;

            if (!isExists)
            {
                await _dbContext.DbAssessmentAssessData.AddAsync(assessData);
            }
        }


        private async Task PersistWorkflowDataToVerifyFromAssess(
            int processId,
            int workflowInstanceId)
        {
            LogContext.PushProperty("PersistWorkflowDataToVerifyFromAssess", nameof(PersistWorkflowDataToVerifyFromAssess));
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("WorkflowInstanceId", workflowInstanceId);

            _logger.LogInformation("Entering {PersistWorkflowDataToVerifyFromAssess} with processId: {ProcessId} and workflowInstanceId: {WorkflowInstanceId}.");

            var assessData = await _dbContext.DbAssessmentAssessData.SingleAsync(d => d.ProcessId == processId);

            _logger.LogInformation("Saving task data from assess to verify for processId: {ProcessId} and workflowInstanceId: {WorkflowInstanceId}.");


            var verifyData = await _dbContext.DbAssessmentVerifyData.SingleOrDefaultAsync(d => d.ProcessId == processId);

            var isExists = (verifyData != null);
            if (!isExists)
            {
                verifyData = new DbAssessmentVerifyData();
            }

            verifyData.ProcessId = processId;
            verifyData.WorkflowInstanceId = workflowInstanceId;

            verifyData.ActivityCode = assessData.ActivityCode;
            verifyData.Ion = assessData.Ion;
            verifyData.SourceCategory = assessData.SourceCategory;
            verifyData.WorkspaceAffected = assessData.WorkspaceAffected;
            verifyData.TaskType = assessData.TaskType;
            verifyData.ProductActioned = assessData.ProductActioned;
            verifyData.ProductActionChangeDetails = assessData.ProductActionChangeDetails;
            verifyData.Reviewer = assessData.Reviewer;
            verifyData.Assessor = assessData.Assessor;
            verifyData.Verifier = assessData.Verifier;

            if (!isExists)
            {
                await _dbContext.DbAssessmentVerifyData.AddAsync(verifyData);
            }
        }
    }
}
