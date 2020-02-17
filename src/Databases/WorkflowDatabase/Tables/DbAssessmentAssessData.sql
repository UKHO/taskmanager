CREATE TABLE [dbo].[DbAssessmentAssessData]
(
	[DbAssessmentAssessDataId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[ProcessId] INT NOT NULL, 
	[Ion] NVARCHAR(50) NULL, 
	[ActivityCode] NVARCHAR(50) NULL, 
	[SourceCategory] NVARCHAR(255) NULL, 
	[Reviewer] NVARCHAR(255) NULL, 
	[Assessor] NVARCHAR(255) NULL, 
	[Verifier] NVARCHAR(255) NULL, 
	[TaskType] NVARCHAR(50) NULL, 
	[WorkflowInstanceId] INT NOT NULL, 
	[ProductActioned] BIT NULL, 
	[ProductActionChangeDetails] NVARCHAR(MAX) NULL, 
	[WorkspaceAffected] NVARCHAR(100) NULL, 
    CONSTRAINT [FK_DbAssessmentAssessData_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [WorkflowInstance]([WorkflowInstanceId])
)
