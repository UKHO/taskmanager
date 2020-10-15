CREATE TABLE [dbo].[CachedHpdEncProduct]
(
    [CachedHpdEncProductId] INT NOT NULL PRIMARY KEY IDENTITY,
    [Name] NVARCHAR(100) NOT NULL
)

GO

CREATE UNIQUE INDEX [IX_CachedHpdEncProduct_Name] ON [dbo].[CachedHpdEncProduct] ([Name])
