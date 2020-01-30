CREATE TABLE [dbo].[TaskStageType]
(
	[TaskStageTypeId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] NVARCHAR(50) NOT NULL, 
    [SequenceNumber] INT NOT NULL, 
    [AllowRework] BIT NOT NULL,
	
)
