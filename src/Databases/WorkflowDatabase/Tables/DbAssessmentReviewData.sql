CREATE TABLE [dbo].[DbAssessmentReviewData]
(
	[DbAssessmentReviewDataId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProcessId] INT NOT NULL, 
    [Ion] NVARCHAR(50) NULL, 
    [ActivityCode] NVARCHAR(50) NULL, 
    [Assessor] NVARCHAR(255) NULL, 
    [Verifier] NVARCHAR(255) NULL, 
    [TaskComplexity] NVARCHAR(50) NULL, 
    [WorkflowInstanceId] INT NOT NULL, 
    CONSTRAINT [FK_DbAssessmentReviewData_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [WorkflowInstance]([WorkflowInstanceId])
)
