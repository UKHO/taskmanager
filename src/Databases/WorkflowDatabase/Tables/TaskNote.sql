CREATE TABLE [dbo].[TaskNote]
(
	[TaskNoteId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProcessId] INT NOT NULL, 
    [Text] NVARCHAR(MAX) NOT NULL, 
    [WorkflowInstanceId] INT NOT NULL, 
    [CreatedByAdUserId] INT NOT NULL, 
    [Created] DATETIME NOT NULL, 
    [LastModified] DATETIME NOT NULL, 
    [LastModifiedByAdUserId] INT NOT NULL, 
    CONSTRAINT [FK_TaskNote_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [WorkflowInstance]([WorkflowInstanceId]),   
    CONSTRAINT [FK_TaskNote_CreatedByAdUserId] FOREIGN KEY ([CreatedByAdUserId]) REFERENCES [AdUsers]([AdUserId]),
    CONSTRAINT [FK_TaskNote_LastModifiedByUserId] FOREIGN KEY ([LastModifiedByAdUserId]) REFERENCES [AdUsers]([AdUserId])

)
