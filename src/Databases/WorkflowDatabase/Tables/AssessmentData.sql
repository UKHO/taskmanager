CREATE TABLE [dbo].[AssessmentData]
(
			[AssessmentDataId] INT NOT NULL PRIMARY KEY IDENTITY,
			[PrimarySdocId] INT NOT NULL,
			[RsdraNumber] NVARCHAR(50) NOT NULL,
			[SourceDocumentName] NVARCHAR(255) NOT NULL,
			[ReceiptDate] DATETIME2 NOT NULL,
			[ToSdoDate] DATETIME2 NULL,
			[EffectiveStartDate] DATETIME2 NULL,
			[TeamDistributedTo] NVARCHAR(20) NULL,
			[SourceDocumentType] NVARCHAR(4000) NULL,
			[SourceNature] NVARCHAR(255) NULL,
			[Datum] NVARCHAR(2000) NULL, 
			[ProcessId] INT NOT NULL, 
    CONSTRAINT [AK_AssessmentData_ProcessId] UNIQUE ([ProcessId]), 
    CONSTRAINT [FK_AssessmentData_WorkflowInstance] FOREIGN KEY ([ProcessId]) REFERENCES [WorkflowInstance]([ProcessId]), 
    CONSTRAINT [AK_AssessmentData_SdocId] UNIQUE ([PrimarySdocId])
)

