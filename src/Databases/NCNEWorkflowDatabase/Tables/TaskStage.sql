CREATE TABLE [dbo].[TaskStage]
(
	[TaskStageId] INT NOT NULL IDENTITY,
	[ProcessId] INT NOT NULL,
	[TaskStageTypeId] INT NOT NULL,
	[DateExpected] DATETIME NULL,
	[DateCompleted] DATETIME NULL,
	[Status] NVARCHAR(25) NOT NULL,
	CONSTRAINT [PK_TaskStage] PRIMARY KEY(ProcessId, TaskStageId),	
	CONSTRAINT [AK_TaskStage] UNIQUE (ProcessId, TaskStageTypeId),
	CONSTRAINT [FK_TaskStage_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [TaskInfo]([ProcessId]),
	CONSTRAINT [FK_TaskStage_StageTypeId] FOREIGN KEY ([TaskStageTypeId]) REFERENCES [TaskStageType]([TaskStageTypeId])
	)
