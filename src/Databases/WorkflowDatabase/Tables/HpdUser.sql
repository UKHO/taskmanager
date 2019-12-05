CREATE TABLE [dbo].[HpdUser]
(
	[HpdUserId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[AdUsername] NVARCHAR(255) NOT NULL, 
	[HpdUsername] NVARCHAR(255) NOT NULL, 
	CONSTRAINT [AK_HpdUser_AdUsername] UNIQUE ([AdUsername]),
	CONSTRAINT [AK_HpdUser_HpdUsername] UNIQUE ([HpdUsername])
)

