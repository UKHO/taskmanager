CREATE TABLE [dbo].[Comment]
(
	[CommentId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProcessId] INT NOT NULL, 
    [Text] NVARCHAR(4000) NOT NULL, 
    [WorkflowInstanceId] INT NOT NULL, 
    [Username] NVARCHAR(255) NOT NULL, 
    [Created] DATETIME NOT NULL, 
    CONSTRAINT [FK_Comment_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [WorkflowInstance]([WorkflowInstanceId])
)
