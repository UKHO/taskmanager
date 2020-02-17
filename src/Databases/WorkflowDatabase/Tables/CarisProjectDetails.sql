CREATE TABLE [dbo].[CarisProjectDetails]
(
	[CarisProjectDetailsId] INT NOT NULL PRIMARY KEY, 
    [ProcessId] INT NOT NULL,
    [ProjectId] INT NOT NULL, 
    [ProjectName] NVARCHAR(100) NOT NULL, 
    [Workspace] NVARCHAR(100) NOT NULL, 
    [Created] DATETIME NOT NULL, 
    [CreatedBy] NVARCHAR(255) NOT NULL, 
    
)
