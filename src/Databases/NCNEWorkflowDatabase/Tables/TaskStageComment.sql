CREATE TABLE [dbo].[TaskStageComment]
(
	[TaskStageCommentId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[TaskStageId] INT NOT NULL, 
	[ProcessId] INT NOT NULL, 
	[Comment] NVARCHAR(4000) NOT NULL, 
	[AdUserId] INT NOT NULL, 
	[Created] DATETIME NOT NULL,
    CONSTRAINT [FK_TaskStage_Comment] FOREIGN KEY ([ProcessId], [TaskStageId]) REFERENCES [TaskStage]( [ProcessId],[TaskStageId]),
	CONSTRAINT [FK_TaskStage_AdUserId] FOREIGN KEY ([AdUserId]) REFERENCES [AdUser]([AdUserId])
)
