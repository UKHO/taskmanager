CREATE TABLE [dbo].[DbAssessmentAssignTask]
(
	[DbAssessmentAssignTaskId] INT NOT NULL PRIMARY KEY IDENTITY,
	[ProcessId] INT NOT NULL,
	[Assessor] NVARCHAR(255) NULL, 
    [Verifier] NVARCHAR(255) NULL, 
	[TaskType] NVARCHAR(50) NULL,
	[WorkspaceAffected] NVARCHAR(100) NULL,
	[Notes] NVARCHAR(4000) NULL,
	CONSTRAINT [FK_DbAssessmentAssignTask_WorkflowInstance] FOREIGN KEY ([ProcessId]) REFERENCES [WorkflowInstance]([ProcessId])
)
