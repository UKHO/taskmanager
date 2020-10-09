﻿CREATE TABLE [dbo].[DbAssessmentAssessData]
(
	[DbAssessmentAssessDataId] INT NOT NULL PRIMARY KEY IDENTITY, 
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
	[ProductActioned] BIT NULL, 
	[ProductActionChangeDetails] NVARCHAR(MAX) NULL, 
	[WorkspaceAffected] NVARCHAR(100) NULL, 
    CONSTRAINT [FK_DbAssessmentAssessData_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [WorkflowInstance]([WorkflowInstanceId]),
	CONSTRAINT [FK_DbAssessmentAssessData_ReviewerAdUserId] FOREIGN KEY ([ReviewerAdUserId]) REFERENCES [AdUsers]([AdUserId]),
	CONSTRAINT [FK_DbAssessmentAssessData_AssessorAdUserId] FOREIGN KEY ([AssessorAdUserId]) REFERENCES [AdUsers]([AdUserId]),
	CONSTRAINT [FK_DbAssessmentAssessData_VerifierAdUserId] FOREIGN KEY ([VerifierAdUserId]) REFERENCES [AdUsers]([AdUserId])
)
