CREATE TABLE [dbo].[ProductAction]
(
	[ProductActionId] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(50) NOT NULL,
	CONSTRAINT [AK_ProductAction_Name] UNIQUE ([Name])
)
