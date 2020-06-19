CREATE TABLE [dbo].[ChartingArea]
(
	[ChartingAreaId] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(50) NOT NULL,
	CONSTRAINT [AK_ChartingArea_Name] UNIQUE ([Name])
	
)
