CREATE TABLE [dbo].[DataImpact]
(
	[DataImpactId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[ProcessId] INT NOT NULL, 
    [HpdUsageId] INT NOT NULL,
    [Edited] BIT NOT NULL, 
	[Comments] NVARCHAR(4000),
	[FeaturesVerified] BIT NOT NULL,
    [FeaturesSubmitted] BIT NOT NULL, 
    CONSTRAINT [FK_DataImpact_WorkflowInstance] FOREIGN KEY ([ProcessId]) REFERENCES [WorkflowInstance]([ProcessId]),
	CONSTRAINT [FK_DataImpact_HpdUsage] FOREIGN KEY ([HpdUsageId]) REFERENCES [HpdUsage]([HpdUsageId])
)
