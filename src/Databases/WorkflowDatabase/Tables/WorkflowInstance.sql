﻿CREATE TABLE [dbo].[WorkflowInstance]
(
	[WorkflowInstanceId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProcessId] INT NOT NULL,  
	[PrimarySdocId] INT NOT NULL,
    [SerialNumber] NVARCHAR(255) NOT NULL, 
    [ParentProcessId] INT NULL, 
    [ActivityName] NVARCHAR(50) NOT NULL, 
    [ActivityChangedAt] DATE NOT NULL, 
	[StartedAt] DATETIME NOT NULL, 
    [Status] NVARCHAR(25) NOT NULL,
    CONSTRAINT [AK_WorkflowInstance_ProcessId] UNIQUE ([ProcessId]),
    CONSTRAINT [AK_WorkflowInstance_PrimarySdocId] UNIQUE ([ProcessId],[PrimarySdocId])
)
