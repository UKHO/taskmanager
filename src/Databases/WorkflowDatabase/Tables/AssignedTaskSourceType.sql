CREATE TABLE [dbo].[AssignedTaskSourceType]
(
	[AssignedTaskSourceTypeId] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(50) NOT NULL, 
    CONSTRAINT [AK_AssignedTaskSourceType_Name] UNIQUE ([Name])
)
