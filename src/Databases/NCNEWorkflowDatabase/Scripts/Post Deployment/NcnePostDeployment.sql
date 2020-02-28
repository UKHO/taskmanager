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

/* Workflow Type */

merge [dbo].[WorkflowType] as target
using (
		values 
				(1,'NE'),
				(2, 'NC'),
				(3, 'CME'),
                (4, 'UNE'),
                (5, 'Refresh'),
                (6, 'Fleet')
) as source ([WorkflowTypeId], [Name])
on (target.[WorkflowTypeId] = source.[WorkflowTypeId])
when matched THEN
UPDATE SET [Name] = source.[Name]
WHEN NOT MATCHED BY target THEN
INSERT     ([WorkflowTypeId], [Name])
     VALUES (source.[WorkflowTypeId], source.[Name])
WHEN NOT MATCHED BY source THEN DELETE;

/* Chart Type */

merge [dbo].[ChartType] as target
using (
		values 
				(1,'Primary'),
				(2, 'Adoption'),
				(3, 'Derived'),
                (4, 'Thematics')
) as source ([ChartTypeId], [Name])
on (target.[ChartTypeId] = source.[ChartTypeId])
when matched THEN
UPDATE SET [Name] = source.[Name]
WHEN NOT MATCHED BY target THEN
INSERT     ([ChartTypeId], [Name])
     VALUES (source.[ChartTypeId], source.[Name])
WHEN NOT MATCHED BY source THEN DELETE;

/* HpdUser */

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