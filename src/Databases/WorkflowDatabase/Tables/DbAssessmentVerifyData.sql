CREATE TABLE [dbo].[DbAssessmentVerifyData]
(
	[DbAssessmentVerifyDataId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[ProcessId] INT NOT NULL, 
	[Ion] NVARCHAR(50) NULL, 
	[ActivityCode] NVARCHAR(50) NULL, 
	[Complexity] NVARCHAR(50) NULL, 
	[SourceCategory] NVARCHAR(255) NULL, 
	[ReviewerAdUserId] INT NULL, 
	[AssessorAdUserId] INT NULL, 
	[VerifierAdUserId] INT NULL, 
	[TaskType] NVARCHAR(50) NULL, 
	[WorkflowInstanceId] INT NOT NULL, 
	[ProductActioned] BIT NOT NULL DEFAULT 0, 
	[ProductActionChangeDetails] NVARCHAR(MAX) NULL, 
	[SncActioned] BIT NOT NULL DEFAULT 0, 
	[SncActionChangeDetails] NVARCHAR(MAX) NULL, 
	[WorkspaceAffected] NVARCHAR(100) NULL,
	CONSTRAINT [FK_DbAssessmentVerifyData_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [WorkflowInstance]([WorkflowInstanceId]),
	CONSTRAINT [FK_DbAssessmentVerifyData_ReviewerAdUserId] FOREIGN KEY ([ReviewerAdUserId]) REFERENCES [AdUsers]([AdUserId]),
	CONSTRAINT [FK_DbAssessmentVerifyData_AssessorAdUserId] FOREIGN KEY ([AssessorAdUserId]) REFERENCES [AdUsers]([AdUserId]),
	CONSTRAINT [FK_DbAssessmentVerifyData_VerifierAdUserId] FOREIGN KEY ([VerifierAdUserId]) REFERENCES [AdUsers]([AdUserId])
)
