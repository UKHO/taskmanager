CREATE TABLE [dbo].[CachedHpdWorkspaces]
(
	[CachedHpdWorkspacesId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] NVARCHAR(100) NOT NULL
)

GO

CREATE UNIQUE INDEX [IX_CachedHpdWorkspaces_Name] ON [dbo].[CachedHpdWorkspaces] ([Name])
