CREATE TABLE [dbo].[Comments]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [ProcessId] INT NOT NULL, 
    [Comment] NVARCHAR(50) NOT NULL
)
