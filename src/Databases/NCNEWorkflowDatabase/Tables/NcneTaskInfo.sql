CREATE TABLE [dbo].[NcneTaskInfo]
(
	[ProcessId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Ion] NVARCHAR(50) NULL, 
    [ChartNumber] INT NULL, 
    [Country] NVARCHAR(75) NULL, 
    [ChartType] NVARCHAR(10) NULL, 
    [WorkflowType] NVARCHAR(10) NULL, 
    [Dating] NVARCHAR(10) NULL, 
    [PublicationDate] DATETIME NULL, 
    [AnnounceDate] DATETIME NULL, 
    [CommitDate] DATETIME NULL, 
    [CISDate] DATETIME NULL, 
    [Compiler] NVARCHAR(50) NULL, 
    [VerifierOne] NVARCHAR(50) NULL, 
    [VerifierTwo] NVARCHAR(50) NULL, 
    [Publisher] NVARCHAR(50) NULL
)
