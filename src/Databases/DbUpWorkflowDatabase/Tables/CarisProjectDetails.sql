CREATE TABLE [dbo].[CarisProjectDetails]
(
	[CarisProjectDetailsId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProcessId] INT NOT NULL,
    [ProjectId] INT NOT NULL, 
    [ProjectName] NVARCHAR(100) NOT NULL, 
    [Created] DATETIME NOT NULL, 
    [CreatedByAdUserId] INT NOT NULL, 
    CONSTRAINT [AK_CarisProjectDetails_ProcessId] UNIQUE ([ProcessId]), 
    CONSTRAINT [FK_CarisProjectDetails_TaskInfo] FOREIGN KEY ([ProcessId]) REFERENCES [TaskInfo]([ProcessId]),
    CONSTRAINT [FK_CarisProjectDetails_CreatedByAdUserId] FOREIGN KEY ([CreatedByAdUserId]) REFERENCES [AdUser]([AdUserId])
)
