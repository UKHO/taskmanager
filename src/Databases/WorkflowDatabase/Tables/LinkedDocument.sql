CREATE TABLE [dbo].[LinkedDocument]
(
	[LinkedDocumentId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[ProcessId] INT NOT NULL, 
    [PrimarySdocId] INT NOT NULL, 
    [RsdraNumber] NVARCHAR(50) NOT NULL, 
    [SourceDocumentName] NVARCHAR(255) NOT NULL, 
	[ReceiptDate] DATETIME2 NULL,
	[SourceDocumentType] NVARCHAR(4000) NULL,
	[SourceNature] NVARCHAR(255) NULL,
	[Datum] NVARCHAR(2000) NULL, 
    [LinkType] NVARCHAR(10) NOT NULL, 
    [LinkedSdocId] INT NOT NULL,
    [ContentServiceId] UNIQUEIDENTIFIER NULL, 
    [Status] NVARCHAR(25) NOT NULL,
    [Created] DATETIME NOT NULL, 
    CONSTRAINT [FK_LinkedDocument_WorkflowInstance] FOREIGN KEY ([ProcessId]) REFERENCES [WorkflowInstance]([ProcessId])
)
