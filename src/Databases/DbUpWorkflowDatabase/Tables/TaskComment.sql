CREATE TABLE [dbo].[TaskComment]
(
	[TaskCommentId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProcessId] INT NOT NULL, 
    [Comment] NVARCHAR(4000) NOT NULL, 
    [Username] NVARCHAR(255) NOT NULL, 
    [Created] DATETIME NOT NULL, 
    [ActionIndicator] BIT NOT NULL, 
    [ActionRole] NVARCHAR(50) NULL,
    CONSTRAINT [FK_TaskComment_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [TaskInfo]([ProcessId])
)
