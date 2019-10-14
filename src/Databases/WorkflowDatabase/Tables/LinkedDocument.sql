CREATE TABLE [dbo].[LinkedDocument]
(
	[LinkedDocumentId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [SdocId] INT NOT NULL, 
    [RsdraNumber] NVARCHAR(50) NOT NULL, 
    [SourceDocumentName] NVARCHAR(255) NOT NULL, 
    [LinkType] NVARCHAR(10) NOT NULL, 
    [LinkedSdocId] INT NOT NULL, 
    [Created] DATETIME NOT NULL
)
