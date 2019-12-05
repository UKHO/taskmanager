CREATE TABLE [dbo].[ProductActionType]
(
	[ProductActionTypeId] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(255) NOT NULL, 
    CONSTRAINT [AK_ProductActionType_Name] UNIQUE ([Name])
)
