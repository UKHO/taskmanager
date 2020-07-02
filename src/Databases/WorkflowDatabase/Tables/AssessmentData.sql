CREATE TABLE [dbo].[AssessmentData]
(
			[AssessmentDataId] INT NOT NULL PRIMARY KEY IDENTITY,
			[ProcessId] INT NOT NULL, 
			[PrimarySdocId] INT NOT NULL,
			[RsdraNumber] NVARCHAR(50) NULL,
			[SourceDocumentName] NVARCHAR(255) NULL,
			[ReceiptDate] DATETIME2 NULL,
			[ToSdoDate] DATETIME2 NULL,
			[EffectiveStartDate] DATETIME2 NULL,
			[TeamDistributedTo] NVARCHAR(50) NULL,
			[SourceDocumentType] NVARCHAR(4000) NULL,
			[SourceNature] NVARCHAR(255) NULL,
			[Datum] NVARCHAR(2000) NULL
    CONSTRAINT [AK_AssessmentData_ProcessId] UNIQUE ([ProcessId]), 
    CONSTRAINT [FK_AssessmentData_WorkflowInstance] FOREIGN KEY ([ProcessId]) REFERENCES [WorkflowInstance]([ProcessId])
)

