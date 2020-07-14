CREATE TABLE [dbo].[TaskNote]
(
	[TaskNoteId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[ProcessId] INT NOT NULL, 
	[Text] NVARCHAR(MAX) NOT NULL, 
	[CreatedByAdUserId] INT NOT NULL, 
	[Created] DATETIME NOT NULL, 
	[LastModified] DATETIME NOT NULL, 
	[LastModifiedByAdUserId] INT NOT NULL , 
	CONSTRAINT [AK_TaskNote_ProcessId] UNIQUE ([ProcessId]),
	CONSTRAINT [FK_TaskNote_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [TaskInfo]([ProcessId]),
	CONSTRAINT [FK_TaskNote_CreatedUser] FOREIGN KEY ([CreatedByAdUserId]) REFERENCES [AdUser]([AdUserId]),
	CONSTRAINT [FK_TaskNote_ModifiedUser] FOREIGN KEY ([LastModifiedByAdUserId]) REFERENCES [AdUser]([AdUserId])
)
