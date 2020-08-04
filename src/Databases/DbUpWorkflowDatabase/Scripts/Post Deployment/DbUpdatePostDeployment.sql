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

/* Charting Area */

merge [dbo].[ChartingArea] as target
using (
		values 
				(1,'Home waters'),
				(2, 'National responsiblity'),
				(3, 'Primary charting')
) as source ([ChartingAreaId], [Name])
on (target.[ChartingAreaId] = source.[ChartingAreaId])
when matched THEN
UPDATE SET [Name] = source.[Name]
WHEN NOT MATCHED BY target THEN
INSERT     ([ChartingAreaId], [Name])
     VALUES (source.[ChartingAreaId], source.[Name])
WHEN NOT MATCHED BY source THEN DELETE;

/* Update Type */

merge [dbo].[UpdateType] as target
using (
		values 
				(1,'Steady State'),
				(2, 'Update from Source'),
				(3, 'Initial Population'),
                (4, 'Wrecks'),
                (5, 'Continuous improvement')
) as source ([UpdateTypeId], [Name])
on (target.[UpdateTypeId] = source.[UpdateTypeId])
when matched THEN
UPDATE SET [Name] = source.[Name]
WHEN NOT MATCHED BY target THEN
INSERT     ([UpdateTypeId], [Name])
     VALUES (source.[UpdateTypeId], source.[Name])
WHEN NOT MATCHED BY source THEN DELETE;


/* Task Stage Type */

merge [dbo].[TaskStageType] as target
using (
  values
         (1, 'Compile Database', 1, 0),
         (2, 'Verify Database', 2, 1),
         (3, 'Verification Rework', 3, 0),
         (4, 'SNC', 4, 0),
         (5, 'ENC', 5, 0)

) as source ([TaskStageTypeId], [Name], [SequenceNumber], [AllowRework])
on (target.[TaskStageTypeId] = source.[TaskStageTypeId])
WHEN matched THEN
UPDATE SET [Name] = source.[Name],
           [SequenceNumber] = source.[SequenceNumber],
           [AllowRework] = source.[AllowRework]
WHEN NOT MATCHED BY target THEN
INSERT ([TaskStageTypeId], [Name], [SequenceNumber], [AllowRework])
      VALUES (source.[TaskStageTypeId], source.[Name], source.[SequenceNumber], source.[AllowRework])
WHEN NOT MATCHED BY source THEN DELETE;
  

/* Product Action */
merge [dbo].[ProductAction] as target
using (
    values
        (1, 'None'),
        (2, 'SNC'),
        (3, 'ENC'),
        (4, 'SNC & ENC')
) as source ([ProductActionId], [Name])
on (target.[ProductActionId] = source.[ProductActionId])
WHEN matched THEN
UPDATE SET [Name] = source.[Name]
WHEN NOT MATCHED BY target THEN
INSERT ([ProductActionId], [Name])
    VALUES (source.[ProductActionId], source.[Name])
WHEN NOT MATCHED BY source THEN DELETE;

