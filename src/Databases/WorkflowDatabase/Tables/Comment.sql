CREATE TABLE [dbo].[Comment]
(
	[CommentId] INT NOT NULL PRIMARY KEY, 
    [WorkflowProcessId] INT NOT NULL, 
    [Text] NVARCHAR(50) NOT NULL
)
