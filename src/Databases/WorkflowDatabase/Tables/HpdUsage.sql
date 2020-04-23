CREATE TABLE [dbo].[HpdUsage]
(
	[HpdUsageId] INT NOT NULL, 
	[Name] NVARCHAR(255) NOT NULL, 
	[SortIndex] TINYINT NOT NULL, 
    CONSTRAINT [PK_HpdUsage] PRIMARY KEY ([HpdUsageId]),
	CONSTRAINT [AK_HpdUsage_Name] UNIQUE ([Name]),
	CONSTRAINT [AK_HpdUsage_SortIndex] UNIQUE ([SortIndex])
)

