CREATE TABLE [dbo].[TaskStageComment]
(
	[TaskStageCommentId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [TaskStageId] INT NOT NULL, 
    [ProcessId] INT NOT NULL, 
    [Comment] NVARCHAR(4000) NOT NULL, 
    [Username] NVARCHAR(255) NOT NULL, 
    [Created] DATETIME NOT NULL,
    CONSTRAINT [FK_TaskStage_Comment] FOREIGN KEY ([ProcessId], [TaskStageId]) REFERENCES [TaskStage]( [ProcessId],[TaskStageId])
)
