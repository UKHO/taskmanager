/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
merge [dbo].[AssignedTaskSourceType] as target
using (
		values 
				(1,'Simple'),
				(2, 'LTA (Product only)'),
				(3, 'LTA')
) as source ([AssignedTaskSourceTypeId], [Name])
on (target.[AssignedTaskSourceTypeId] = source.[AssignedTaskSourceTypeId])
when matched THEN
UPDATE SET [Name] = source.[Name]
WHEN NOT MATCHED BY target THEN
INSERT     ([AssignedTaskSourceTypeId], [Name])
     VALUES (source.[AssignedTaskSourceTypeId], source.[Name])
WHEN NOT MATCHED BY source THEN DELETE;