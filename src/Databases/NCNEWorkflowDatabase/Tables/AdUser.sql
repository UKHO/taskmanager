CREATE TABLE [dbo].[AdUser]
(
	[AdUserId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [DisplayName] NVARCHAR(255) NOT NULL, 
    [ActiveDirectorySid] NVARCHAR(255) NOT NULL, 
    [LastCheckedDate] DATETIME NOT NULL
)

GO

CREATE UNIQUE INDEX [IX_AdUser_ActiveDirectorySid] ON [dbo].[AdUser] ([ActiveDirectorySid])
