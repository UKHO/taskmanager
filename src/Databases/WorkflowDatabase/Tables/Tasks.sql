CREATE TABLE [dbo].[Tasks]
(
	[TaskId] INT NOT NULL PRIMARY KEY,
    [WorkflowProcessId] INT NOT NULL, 
    [DaysToDmEndDate] SMALLINT NULL, 
    [DmEndDate] DATETIME2 NULL, 
    [DaysOnHold] SMALLINT NULL, 
    [RsdraNo] NVARCHAR(50) NULL, 
    [SourceName] NVARCHAR(50) NULL, 
    [Workspace] NVARCHAR(10) NULL, 
    [TaskType] NVARCHAR(10) NULL, 
    [TaskStage] NVARCHAR(10) NULL, 
    [Assessor] NVARCHAR(10) NULL, 
    [Verifier] NVARCHAR(10) NULL, 
    [Team] NVARCHAR(10) NULL, 
    [TaskNote] NVARCHAR(100) NULL, 
)
