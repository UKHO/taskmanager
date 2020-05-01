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


/* Task Stage Type */

merge [dbo].[TaskStageType] as target
using (
  values
       
         (1, 'With SDRA', 1, 0),
         (2, 'With Geodesy', 2, 0),
         (3, 'Specification', 3, 0),
         (4, 'Compile Chart', 4, 0),
         (5, 'V1', 5, 1),
         (6, 'V1 Rework', 6, 0),
         (7, 'V2', 7, 1),
         (8, 'V2 Rework', 8, 0),
         (9, 'Forms', 9, 0),
         (10, 'Final updating', 10, 0),
         (11, '100% check', 11, 0),
         (12, 'Commit to print', 12, 0),
         (13, 'CIS', 13, 0),
         (14, 'Publication', 14, 0),
         (15, 'Publish chart', 15, 0),
         (16, 'Clear vector', 16, 0),
         (17, 'Retire old minor version', 17, 0),
         (18, 'Consider withdrawn charts', 18, 0)

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
  



