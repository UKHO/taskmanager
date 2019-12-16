CREATE TABLE [dbo].[CachedHpdWorkspace]
(
	[CachedHpdWorkspaceId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] NVARCHAR(100) NOT NULL
)

GO

CREATE UNIQUE INDEX [IX_CachedHpdWorkspace_Name] ON [dbo].[CachedHpdWorkspace] ([Name])
