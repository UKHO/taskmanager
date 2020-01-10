CREATE TABLE [dbo].[DbAssessmentReviewData]
(
	[DbAssessmentReviewDataId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProcessId] INT NOT NULL, 
    [Ion] NVARCHAR(50) NULL, 
    [ActivityCode] NVARCHAR(50) NULL, 
    [SourceCategory] NVARCHAR(255) NULL, 
    [WorkflowInstanceId] INT NOT NULL, 
	[Assessor] NVARCHAR(255) NULL, 
    [Verifier] NVARCHAR(255) NULL, 
	[TaskType] NVARCHAR(50) NULL,
	[WorkspaceAffected] NVARCHAR(100) NULL,
    [Reviewer] NVARCHAR(255) NULL, 
	[Notes] NVARCHAR(4000) NULL,
    CONSTRAINT [FK_DbAssessmentReviewData_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [WorkflowInstance]([WorkflowInstanceId])
)
