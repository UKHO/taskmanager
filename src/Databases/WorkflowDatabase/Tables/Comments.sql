CREATE TABLE [dbo].[Comments]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [ProcessId] INT NOT NULL, 
    [Text] NVARCHAR(50) NOT NULL
)
