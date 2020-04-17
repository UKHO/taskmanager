CREATE TABLE [dbo].[PrimaryDocumentStatus]
(
	[PrimaryDocumentStatusId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProcessId] INT NOT NULL, 
    [SdocId] INT NOT NULL, 
    [ContentServiceId] UNIQUEIDENTIFIER NULL, 
    [Status] NVARCHAR(25) NOT NULL, 
    [StartedAt] DATETIME NOT NULL, 
    [CorrelationId] UNIQUEIDENTIFIER NULL, 
	[Filename] NVARCHAR(100) NULL, 
	[Filepath] NVARCHAR(255) NULL, 
    CONSTRAINT [FK_PrimaryDocumentStatus_WorkflowInstance] FOREIGN KEY ([ProcessId]) REFERENCES [WorkflowInstance]([ProcessId]), 
    CONSTRAINT [AK_PrimaryDocumentStatus_ProcessId] UNIQUE ([ProcessId])
)
