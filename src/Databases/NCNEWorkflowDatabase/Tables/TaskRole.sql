CREATE TABLE [dbo].[TaskRole]
(
	[TaskRoleId] INT NOT NULL PRIMARY KEY IDENTITY,
	[ProcessId] INT NOT NULL,	
	[CompilerAdUserId] INT NULL, 
    [VerifierOneAdUserId] INT NULL, 
    [VerifierTwoAdUserId] INT NULL, 
    [HundredPercentCheckAdUserId] INT NULL, 
    CONSTRAINT [AK_TaskRole_ProcessId] UNIQUE ([ProcessId]),
    CONSTRAINT [FK_TaskRole_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [TaskInfo]([ProcessId]),
	CONSTRAINT [FK_TaskRole_Compiler] FOREIGN KEY ([CompilerAdUserId]) REFERENCES [AdUser]([AdUserId]),
	CONSTRAINT [FK_TaskRole_VerifierOne] FOREIGN KEY ([VerifierOneAdUserId]) REFERENCES [AdUser]([AdUserId]),
	CONSTRAINT [FK_TaskRole_VerifierTwo] FOREIGN KEY ([VerifierTwoAdUserId]) REFERENCES [AdUser]([AdUserId]),
	CONSTRAINT [FK_TaskRole_HundredPCheck] FOREIGN KEY ([HundredPercentCheckAdUserId]) REFERENCES [AdUser]([AdUserId])

)
