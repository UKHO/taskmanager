CREATE TABLE [dbo].[ProductAction]
(
	[ProductActionId] INT NOT NULL PRIMARY KEY, 
    [Name] NCHAR(50) NOT NULL,
	CONSTRAINT [AK_Product_Action_Name] UNIQUE ([Name])
)
