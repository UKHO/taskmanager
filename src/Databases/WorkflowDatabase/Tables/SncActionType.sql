CREATE TABLE [dbo].[SncActionType]
(
	[SncActionTypeId] INT NOT NULL PRIMARY KEY, 
	[Name] NCHAR(255) NOT NULL,
	CONSTRAINT [AK_SncActionType_Name] UNIQUE ([Name])
)
