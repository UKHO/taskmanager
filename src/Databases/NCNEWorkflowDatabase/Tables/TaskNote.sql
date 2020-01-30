CREATE TABLE [dbo].[TaskNote]
(
	[TaskNoteId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[ProcessId] INT NOT NULL, 
	[Text] NVARCHAR(MAX) NOT NULL, 
	[CreatedByUsername] NVARCHAR(255) NOT NULL, 
	[Created] DATETIME NOT NULL, 
	[LastModified] DATETIME NOT NULL, 
	[LastModifiedByUsername] NVARCHAR(255) NOT NULL , 
	CONSTRAINT [FK_TaskNote_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [TaskInfo]([ProcessId])
)
