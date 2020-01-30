CREATE TABLE [dbo].[WorkflowType]
(
	[WorkflowTypeId] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(50) NOT NULL,
	CONSTRAINT [AK_WorkflowType_Name] UNIQUE ([Name])
	
)
