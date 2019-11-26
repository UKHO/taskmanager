CREATE TABLE [dbo].[HpdUsages]
(
	[HpdUsageId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[Name] NVARCHAR(255) NOT NULL 
)

GO

CREATE UNIQUE INDEX [UQ_HpdUsages_Name] ON [dbo].[HpdUsages] ([Name])
