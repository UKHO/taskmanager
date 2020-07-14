CREATE TABLE [dbo].[TaskRole]
(
	[TaskRoleId] INT NOT NULL PRIMARY KEY IDENTITY,
	[ProcessId] INT NOT NULL,	
	[CompilerAdUserId] INT NOT NULL, 
    [VerifierAdUserId] INT NULL, 
    CONSTRAINT [AK_TaskRole_ProcessId] UNIQUE ([ProcessId]),
    CONSTRAINT [FK_TaskRole_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [TaskInfo]([ProcessId]),
	CONSTRAINT [FK_TaskRole_Compiler] FOREIGN KEY ([CompilerAdUserId]) REFERENCES [AdUser]([AdUserId]),
	CONSTRAINT [FK_TaskRole_Verifier] FOREIGN KEY ([VerifierAdUserId]) REFERENCES [AdUser]([AdUserId])

)
