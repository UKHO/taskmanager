CREATE TABLE [dbo].[HpdUsage]
(
	[HpdUsageId] INT NOT NULL  IDENTITY, 
	[Name] NVARCHAR(255) NOT NULL, 
    CONSTRAINT [PK_HpdUsage] PRIMARY KEY ([HpdUsageId]),
	CONSTRAINT [AK_HpdUsage_Name] UNIQUE ([Name])
)

