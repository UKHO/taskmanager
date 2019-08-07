CREATE TABLE [dbo].[AssessmentData]
(
	[AssessmentDataId] INT NOT NULL PRIMARY KEY,
	[SdocId] INT NOT NULL,
	[RsdraNumber] NVARCHAR(50) NOT NULL,
	[SourceDocumentName] NVARCHAR(255) NOT NULL,
	[ReceiptDate] DATETIME NOT NULL,
	[ToSdoDate] DATETIME NULL,
	[EffectiveStartDate] DATETIME NULL,
	[TeamDistributedTo] NVARCHAR(10) NULL,
	[SourceDocumentType] NVARCHAR(255) NULL,
	[SourceNature] NVARCHAR(20) NULL,
	[Datum] NVARCHAR(20) NULL
)

