CREATE TABLE [dbo].[LinkedDocument]
(
	[LinkedDocumentId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [SdocId] INT NOT NULL, 
    [RsdraNumber] NVARCHAR(50) NOT NULL, 
    [SourceDocumentName] NVARCHAR(255) NOT NULL, 
	[ReceiptDate] DATETIME NULL,
	[SourceDocumentType] NVARCHAR(4000) NULL,
	[SourceNature] NVARCHAR(255) NULL,
	[Datum] NVARCHAR(2000) NULL, 
    [LinkType] NVARCHAR(10) NOT NULL, 
    [LinkedSdocId] INT NOT NULL, 
    [Created] DATETIME NOT NULL, 
    CONSTRAINT [FK_LinkedDocument_AssessmentData] FOREIGN KEY ([SdocId]) REFERENCES [AssessmentData]([SdocId])
)
