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
merge [dbo].[AssignedTaskType] as target
using (
		values 
				(1,'Simple'),
				(2, 'LTA (Product only)'),
				(3, 'LTA')
) as source ([AssignedTaskTypeId], [Name])
on (target.[AssignedTaskTypeId] = source.[AssignedTaskTypeId])
when matched THEN
UPDATE SET [Name] = source.[Name]
WHEN NOT MATCHED BY target THEN
INSERT     ([AssignedTaskTypeId], [Name])
     VALUES (source.[AssignedTaskTypeId], source.[Name])
WHEN NOT MATCHED BY source THEN DELETE;