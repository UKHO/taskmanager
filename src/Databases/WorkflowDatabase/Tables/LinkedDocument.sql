CREATE TABLE [dbo].[LinkedDocument]
(
	[LinkedDocumentId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[ProcessId] INT NOT NULL, 
    [PrimarySdocId] INT NOT NULL, 
    [LinkedSdocId] INT NOT NULL,
    [LinkType] NVARCHAR(10) NOT NULL, 
    [RsdraNumber] NVARCHAR(50) NOT NULL, 
    [SourceDocumentName] NVARCHAR(255) NOT NULL, 
	[ReceiptDate] DATETIME2 NULL,
	[SourceDocumentType] NVARCHAR(4000) NULL,
	[SourceNature] NVARCHAR(255) NULL,
	[Datum] NVARCHAR(2000) NULL, 
    [ContentServiceId] UNIQUEIDENTIFIER NULL, 
    [Status] NVARCHAR(25) NOT NULL,
    [Created] DATETIME NOT NULL,  
	[Filename] NVARCHAR(100) NULL, 
	[Filepath] NVARCHAR(255) NULL,
    CONSTRAINT [FK_LinkedDocument_WorkflowInstance] FOREIGN KEY ([ProcessId]) REFERENCES [WorkflowInstance]([ProcessId]),
    CONSTRAINT [AK_LinkedDocument_Composite_ProcessId_LinkedSdocId_LinkType] UNIQUE ([ProcessId],[LinkedSdocId],[LinkType])

)
