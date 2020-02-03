CREATE TABLE [dbo].[ChartType]
(
	[ChartTypeId] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(50) NOT NULL ,
	CONSTRAINT [AK_ChartType_Name] UNIQUE ([Name])
)
