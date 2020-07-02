CREATE TABLE [dbo].[DatabaseDocumentStatus]
(
	[DatabaseDocumentStatusId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[ProcessId] INT NOT NULL, 
    [SdocId] INT NOT NULL,  
    [RsdraNumber] NVARCHAR(50) NULL, 
    [SourceDocumentName] NVARCHAR(255) NULL, 
	[SourceDocumentType] NVARCHAR(4000) NULL,
	[SourceNature] NVARCHAR(255) NULL,
	[Datum] NVARCHAR(2000) NULL, 
	[ReceiptDate] DATETIME2 NULL,
    [ContentServiceId] UNIQUEIDENTIFIER NULL, 
    [Status] NVARCHAR(25) NOT NULL,
    [Created] DATETIME NOT NULL,      
	[Filename] NVARCHAR(100) NULL, 
	[Filepath] NVARCHAR(255) NULL,
    CONSTRAINT [FK_DatabaseDocumentStatus_WorkflowInstance] FOREIGN KEY ([ProcessId]) REFERENCES [WorkflowInstance]([ProcessId])
)
