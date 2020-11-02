CREATE TABLE [dbo].[SncAction]
(
	[SncActionId] INT NOT NULL PRIMARY KEY IDENTITY,
	[ProcessId] INT NOT NULL, 
	[ImpactedProduct] NVARCHAR(100) NOT NULL, 
	[SncActionTypeId] INT NOT NULL, 
	[Verified] BIT NOT NULL DEFAULT 0, 
	CONSTRAINT [FK_SncAction_SncActionType] FOREIGN KEY ([SncActionTypeId]) REFERENCES [SncActionType]([SncActionTypeId]), 
	CONSTRAINT [FK_SncAction_WorkflowInstance] FOREIGN KEY ([ProcessId]) REFERENCES [WorkflowInstance]([ProcessId])
)
