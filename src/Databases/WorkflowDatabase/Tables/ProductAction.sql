CREATE TABLE [dbo].[ProductAction]
(
	[ProductActionId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProcessId] INT NOT NULL, 
    [ImpactedProduct] NVARCHAR(100) NOT NULL, 
    [ProductActionTypeId] INT NOT NULL, 
    [Verified] BIT NOT NULL DEFAULT 0, 
    CONSTRAINT [FK_ProductAction_ProductActionType] FOREIGN KEY ([ProductActionTypeId]) REFERENCES [ProductActionType]([ProductActionTypeId]), 
    CONSTRAINT [FK_ProductAction_WorkflowInstance] FOREIGN KEY ([ProcessId]) REFERENCES [WorkflowInstance]([ProcessId]), 
    CONSTRAINT [AK_CompositeUnique_ProductAction_ProcessId_ProductActionTypeId] UNIQUE ([ProcessId],[ProductActionTypeId])
)
