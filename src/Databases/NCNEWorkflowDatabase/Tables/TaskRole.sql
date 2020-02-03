CREATE TABLE [dbo].[TaskRole]
(
	[TaskRoleId] INT NOT NULL PRIMARY KEY IDENTITY,
	[ProcessId] INT NOT NULL,	
	[Compiler] NVARCHAR(50) NULL, 
    [VerifierOne] NVARCHAR(50) NULL, 
    [VerifierTwo] NVARCHAR(50) NULL, 
    [Publisher] NVARCHAR(50) NULL, 
    CONSTRAINT [AK_TaskRole_ProcessId] UNIQUE ([ProcessId]),
    CONSTRAINT [FK_TaskRole_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [TaskInfo]([ProcessId])


)
