CREATE TABLE [dbo].[Comments]
(
	[CommentId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProcessId] INT NOT NULL, 
    [Text] NVARCHAR(4000) NOT NULL, 
    [WorkflowInstanceId] INT NOT NULL, 
    [AdUserId] INT NULL, 
    [Created] DATETIME NOT NULL, 
    CONSTRAINT [FK_Comments_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [WorkflowInstance]([WorkflowInstanceId]),
    CONSTRAINT [FK_Comments_AdUser] FOREIGN KEY ([AdUserId]) REFERENCES [AdUsers]([AdUserId])
)

GO

CREATE INDEX [IX_Comments_AdUserId] ON [dbo].[Comments] ([AdUserId])
