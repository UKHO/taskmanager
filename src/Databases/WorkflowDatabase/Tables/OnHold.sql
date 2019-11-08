CREATE TABLE [dbo].[OnHold]
(
	[OnHoldId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[WorkflowInstanceId] INT NOT NULL, 
    [ProcessId] INT NOT NULL, 
    [OnHoldTime] DATETIME NOT NULL, 
	[OffHoldTime] DATETIME NULL, 
	[OnHoldUser] NVARCHAR(255) NOT NULL, 
    [OffHoldUser] NVARCHAR(255) NULL, 
    CONSTRAINT [FK_OnHold_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [WorkflowInstance]([WorkflowInstanceId])
)
