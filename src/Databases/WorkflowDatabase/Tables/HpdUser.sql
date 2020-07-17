CREATE TABLE [dbo].[HpdUser]
(
	[HpdUserId] INT NOT NULL PRIMARY KEY IDENTITY, 
	[AdUserId] INT NOT NULL, 
	[HpdUsername] NVARCHAR(255) NOT NULL, 
    CONSTRAINT [FK_HpdUser_AdUserId] FOREIGN KEY ([AdUserId]) REFERENCES [AdUsers]([AdUserId]),
	CONSTRAINT [AK_HpdUser_HpdUsername] UNIQUE ([HpdUsername]),
	CONSTRAINT [AK_HpdUser_AdUserId] UNIQUE ([AdUserId])
)

