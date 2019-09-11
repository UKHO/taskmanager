CREATE TABLE [dbo].[SourceDocumentStatus]
(
	[SourceDocumentStatusId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProcessId] INT NOT NULL, 
    [SdocId] INT NOT NULL, 
    [ContentServiceId] UNIQUEIDENTIFIER NOT NULL, 
    [Status] NVARCHAR(25) NOT NULL, 
    [StartedAt] DATETIME NOT NULL, 
    CONSTRAINT [FK_SourceDocumentStatus_WorkflowInstance] FOREIGN KEY ([ProcessId]) REFERENCES [WorkflowInstance]([ProcessId]), 
    CONSTRAINT [AK_SourceDocumentStatus_ProcessId] UNIQUE ([ProcessId])
)
