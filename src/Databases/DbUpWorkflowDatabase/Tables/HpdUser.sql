CREATE TABLE [dbo].[HpdUser]
(
	[HpdUserId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[AdUserId] INT NOT NULL, 
	[HpdUsername] NVARCHAR(255) NOT NULL, 
	CONSTRAINT [AK_HpdUser_AdUsername] UNIQUE ([AdUserId], [HpdUsername]),
	CONSTRAINT [AK_HpdUser_AdUserId] UNIQUE ([AdUserId]),
	CONSTRAINT [FK_HpdUser_AdUserId] FOREIGN KEY ([AdUserId]) REFERENCES [AdUser]([AdUserId])
)