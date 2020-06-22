CREATE TABLE [dbo].[UpdateType]
(
	[UpdateTypeId] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(50) NOT NULL ,
	CONSTRAINT [AK_UpdateType_Name] UNIQUE ([Name])
)
