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
merge [dbo].[SncActionType] as target
using (
    values  (1,'Imm Act - Critical NM'),
            (2,'Imm Act - Critical NM Block'),
            (3,'Imm Act - Critical PNM'),
            (4,'Imm Act - Critical TNM'),
            (5,'Imm Act - NM'),
            (6,'Imm Act - NM Block'),
            (7,'Imm Act - PNM'),
            (8,'Imm Act - TNM'),
            (9,'Imm Act - Miscellaneous NM'),
            (10,'LTA'),
            (11,'No action'),
            (12,'Scale too small'),
            (13, 'UNE')
) as source([SncActionTypeId],[Name])
on (target.[SncActionTypeId]=source.[SncActionTypeId])
when matched THEN
UPDATE SET [Name] = source.[Name]
WHEN NOT MATCHED BY target THEN
INSERT     ([SncActionTypeId], [Name])
     VALUES (source.[SncActionTypeId], source.[Name])
WHEN NOT MATCHED BY source THEN DELETE;