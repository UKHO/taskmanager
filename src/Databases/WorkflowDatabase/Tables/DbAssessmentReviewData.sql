CREATE TABLE [dbo].[DbAssessmentReviewData]
(
	[DbAssessmentReviewDataId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProcessId] INT NOT NULL, 
    [Ion] NVARCHAR(50) NULL, 
    [ActivityCode] NVARCHAR(50) NULL, 
    [Complexity] NVARCHAR(50) NULL, 
    [SourceCategory] NVARCHAR(255) NULL, 
    [WorkflowInstanceId] INT NOT NULL, 
    [ReviewerAdUserId] INT NULL, 
	[AssessorAdUserId] INT NULL, 
    [VerifierAdUserId] INT NULL, 
	[TaskType] NVARCHAR(50) NULL,
	[WorkspaceAffected] NVARCHAR(100) NULL,
	[Notes] NVARCHAR(4000) NULL,
    CONSTRAINT [FK_DbAssessmentReviewData_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [WorkflowInstance]([WorkflowInstanceId]),
	CONSTRAINT [FK_DbAssessmentReviewData_ReviewerAdUserId] FOREIGN KEY ([ReviewerAdUserId]) REFERENCES [AdUsers]([AdUserId]),
	CONSTRAINT [FK_DbAssessmentReviewData_AssessorAdUserId] FOREIGN KEY ([AssessorAdUserId]) REFERENCES [AdUsers]([AdUserId]),
	CONSTRAINT [FK_DbAssessmentReviewData_VerifierAdUserId] FOREIGN KEY ([VerifierAdUserId]) REFERENCES [AdUsers]([AdUserId])
)
