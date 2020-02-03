CREATE TABLE [dbo].[TaskInfo]
(
	[ProcessId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Ion] NVARCHAR(50) NULL, 
    [ChartNumber] INT NULL, 
    [Country] NVARCHAR(75) NULL, 
    [ChartType] NVARCHAR(10) NULL, 
    [ChartTitle] NVARCHAR(50) NULL,
    [WorkflowType] NVARCHAR(10) NULL, 
    [Duration] NVARCHAR(10) NULL, 
    [PublicationDate] DATETIME2 NULL, 
    [AnnounceDate] DATETIME2 NOT NULL, 
    [CommitDate] DATETIME2 NOT NULL, 
    [CISDate] DATETIME2 NOT NULL, 
    [SDR] BIT NOT NULL, 
    [Geodesy] BIT NOT NULL, 
    [ThreePs] BIT NOT NULL, 
    [SentDate3Ps] DATETIME2 NULL, 
    [ExpectedDate3Ps] DATETIME2 NULL, 
    [ActualDate3Ps] DATETIME2 NULL, 
    [AssignedUser] NVARCHAR(255) NULL, 
    [AssignedDate] DATETIME2 NULL
)
