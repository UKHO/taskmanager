CREATE TABLE [dbo].[DatabaseDocumentStatus]
(
	[DatabaseDocumentStatusId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[ProcessId] INT NOT NULL, 
    [SdocId] INT NOT NULL, 
    [RsdraNumber] NVARCHAR(50) NOT NULL, 
    [SourceDocumentName] NVARCHAR(255) NOT NULL, 
	[SourceDocumentType] NVARCHAR(4000) NULL,
    [ContentServiceId] UNIQUEIDENTIFIER NULL, 
    [Status] NVARCHAR(25) NOT NULL,
    [Created] DATETIME NOT NULL, 
    CONSTRAINT [FK_DatabaseDocumentStatus_WorkflowInstance] FOREIGN KEY ([ProcessId]) REFERENCES [WorkflowInstance]([ProcessId])
)
