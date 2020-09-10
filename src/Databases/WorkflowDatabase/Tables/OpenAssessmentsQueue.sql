CREATE TABLE [dbo].[OpenAssessmentsQueue]
(
	[OpenAssessmentsQueueId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [PrimarySdocId] INT NOT NULL, 
    [Timestamp] DATETIME NOT NULL
)

GO

CREATE UNIQUE INDEX [IX_OpenAssessmentsQueue_PrimarySdocId] ON [dbo].[OpenAssessmentsQueue] ([PrimarySdocId])
