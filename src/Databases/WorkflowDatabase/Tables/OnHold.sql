CREATE TABLE [dbo].[OnHold]
(
	[OnHoldId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[WorkflowInstanceId] INT NOT NULL, 
    [ProcessId] INT NOT NULL, 
    [OnHoldTime] Date NOT NULL, 
	[OffHoldTime] Date, 
	[OnHoldUser] NVARCHAR(255) NOT NULL, 
    [OffHoldUser] NVARCHAR(255) NULL, 
    CONSTRAINT [FK_OnHold_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [WorkflowInstance]([WorkflowInstanceId])
)
