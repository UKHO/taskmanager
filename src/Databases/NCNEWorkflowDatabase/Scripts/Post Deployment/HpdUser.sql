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
merge [dbo].[HpdUser] as target
using (
		values 
				('Peter Bates','TM_User'),
                ('Matthew Stoodley','TM_User'),
                ('Gareth Evans','TM_User'),
                ('Bonnie Poole','TM_User'),
                ('Rossall Sandford','TM_User'),
                ('Ben Hall','TM_User'),
                ('Samir Hasson','TM_User'),
                ('Greg Williams','TM_User'),
                ('Rajan Shunmuga','TM_User'),
                ('Stuart Barzey','TM_User')
) as source ([AdUsername], [HpdUsername])
on (target.[AdUsername] = source.[AdUsername])
when matched THEN
UPDATE SET [HpdUsername] = source.[HpdUsername]
WHEN NOT MATCHED BY target THEN
INSERT     ([AdUsername], [HpdUsername])
     VALUES (source.[AdUsername], source.[HpdUsername])
WHEN NOT MATCHED BY source THEN DELETE;
