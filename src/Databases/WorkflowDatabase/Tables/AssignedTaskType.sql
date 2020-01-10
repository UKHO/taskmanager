CREATE TABLE [dbo].[AssignedTaskType]
(
	[AssignedTaskTypeId] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(50) NOT NULL, 
    CONSTRAINT [AK_AssignedTaskType_Name] UNIQUE ([Name])
)
