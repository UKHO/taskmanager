CREATE TABLE [dbo].[Process]
(
	[ProcessId] INT NOT NULL PRIMARY KEY, 
    [WorkflowProcessId] INT NOT NULL, 
    [SerialNumber] NVARCHAR(255) NOT NULL, 
    [ParentWorkflowProcessId] INT NULL, 
    [WorkflowType] NVARCHAR(50) NOT NULL, 
    [ActivityName] NVARCHAR(50) NOT NULL
)
