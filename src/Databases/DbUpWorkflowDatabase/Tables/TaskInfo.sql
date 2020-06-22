﻿CREATE TABLE [dbo].[TaskInfo]
(
	[ProcessId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] NVARCHAR(50) NULL,
    [UpdateType] NVARCHAR(50) NULL, 
    [ChartingArea] NVARCHAR(50) NULL,
    [TargetDate] DATETIME2 NULL, 
    [AssignedUser] NVARCHAR(255) NULL, 
    [AssignedDate] DATETIME2 NULL, 
    [CurrentStage] NVARCHAR(25) NULL,
    [Status] NVARCHAR(10) NOT NULL, 
    [StatusChangeDate] DATETIME2 NULL
)
