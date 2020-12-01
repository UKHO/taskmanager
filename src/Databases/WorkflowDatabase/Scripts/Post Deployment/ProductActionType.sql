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
merge [dbo].[ProductActionType] as target
using (
		values 
				(1,'CPTS/IA'),
				(2, 'CPTS/LTA'),
				(3, 'CPTS/LTA M_COVR'),
				(4, 'Product only'),
				(5, 'Scale too small'),
				(6, 'LTA')
) as source ([ProductActionTypeId], [Name])
on (target.[ProductActionTypeId] = source.[ProductActionTypeId])
when matched THEN
UPDATE SET [Name] = source.[Name]
WHEN NOT MATCHED BY target THEN
INSERT     ([ProductActionTypeId], [Name])
     VALUES (source.[ProductActionTypeId], source.[Name])
WHEN NOT MATCHED BY source THEN DELETE;