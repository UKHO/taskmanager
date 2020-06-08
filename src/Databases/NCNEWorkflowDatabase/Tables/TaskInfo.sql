CREATE TABLE [dbo].[TaskInfo]
(
	[ProcessId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Ion] NVARCHAR(50) NULL, 
    [ChartNumber] NVARCHAR(10) NULL, 
    [Country] NVARCHAR(75) NULL, 
    [ChartType] NVARCHAR(10) NULL, 
    [ChartTitle] NVARCHAR(50) NULL,
    [WorkflowType] NVARCHAR(10) NULL, 
    [Duration] NVARCHAR(10) NULL, 
    [PublicationDate] DATETIME2 NULL, 
    [RepromatDate] DATETIME2 NULL,
    [AnnounceDate] DATETIME2 NULL, 
    [CommitDate] DATETIME2 NULL, 
    [CISDate] DATETIME2 NULL, 
    [ThreePs] BIT NOT NULL, 
    [SentDate3Ps] DATETIME2 NULL, 
    [ExpectedDate3Ps] DATETIME2 NULL, 
    [ActualDate3Ps] DATETIME2 NULL, 
    [AssignedUser] NVARCHAR(255) NULL, 
    [AssignedDate] DATETIME2 NULL, 
    [Status] NVARCHAR(10) NOT NULL, 
    [StatusChangeDate] DATETIME2 NULL
)
