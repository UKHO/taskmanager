CREATE TABLE [dbo].[WorkflowInstance]
(
	[WorkflowInstanceId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProcessId] INT NOT NULL, 
    [SerialNumber] NVARCHAR(255) NOT NULL, 
    [ParentProcessId] INT NULL, 
    [WorkflowType] NVARCHAR(50) NOT NULL, 
    [ActivityName] NVARCHAR(50) NOT NULL,
	CONSTRAINT [AK_WorkflowInstance_ProcessId] UNIQUE ([ProcessId])
)
