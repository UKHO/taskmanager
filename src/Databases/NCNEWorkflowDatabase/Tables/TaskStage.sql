CREATE TABLE [dbo].[TaskStage]
(
	[TaskStageId] INT NOT NULL IDENTITY,
	[ProcessId] INT NOT NULL,
	[TaskStageName] NVARCHAR(50) NOT NULL,
	[DateExpected] DATETIME NULL,
	[DateCompleted] DATETIME NULL,
	[Status] NVARCHAR(25) NOT NULL,
	CONSTRAINT [PK_TaskStage] PRIMARY KEY(ProcessId, TaskStageId),
	CONSTRAINT [FK_TaskStage_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [TaskInfo]([ProcessId])
)
