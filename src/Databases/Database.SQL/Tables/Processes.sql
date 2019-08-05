CREATE TABLE [dbo].[Processes]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [ProcessId] INT NOT NULL, 
    [SerialNumber] NVARCHAR(255) NOT NULL, 
    [ParentProcessId] INT NULL, 
    [WorkflowType] NVARCHAR(50) NOT NULL, 
    [ActivityName] NVARCHAR(50) NOT NULL
)
