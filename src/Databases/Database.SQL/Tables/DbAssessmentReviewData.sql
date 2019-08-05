CREATE TABLE [dbo].[DbAssessmentReviewData]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [ProcessId] INT NOT NULL, 
    [Ion] NVARCHAR(50) NULL, 
    [ActivityCode] NVARCHAR(50) NULL, 
    [Assessor] NVARCHAR(255) NULL, 
    [Verifier] NVARCHAR(255) NULL, 
    [TaskComplexity] NVARCHAR(50) NULL
)
