CREATE TABLE [dbo].[OnHold]
(
	[OnHoldId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[WorkflowInstanceId] INT NOT NULL, 
    [ProcessId] INT NOT NULL, 
    [OnHoldTime] Date NOT NULL, 
	[OffHoldTime] Date, 
	[OnHoldByAdUserId] INT NOT NULL, 
    [OffHoldByAdUserId] INT NULL, 
    CONSTRAINT [FK_OnHold_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [WorkflowInstance]([WorkflowInstanceId]),
	CONSTRAINT [FK_OnHoldOnHoldAdUserId] FOREIGN KEY ([OnHoldByAdUserId]) REFERENCES [AdUsers]([AdUserId]),
	CONSTRAINT [FK_OnHoldOffHoldAdUserId] FOREIGN KEY ([OffHoldByAdUserId]) REFERENCES [AdUsers]([AdUserId])

)
