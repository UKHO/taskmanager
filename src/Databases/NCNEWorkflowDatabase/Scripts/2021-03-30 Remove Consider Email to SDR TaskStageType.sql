-- Make sure that there are no current withdrawal tasks in-flight
-- There should be zero rows returned
SELECT *
FROM [taskmanager-dev-ncneworkflowdatabase].[dbo].[TaskInfo]
WHERE [WorkflowType] = 'Withdrawal' AND
	  [Status] NOT IN ('Completed', 'Terminated')

-- Make sure that there are no TaskStageComments that
-- are used by 'Consider Email to SDR' TaskStageType
-- There should be zero rows returned
SELECT *
FROM [taskmanager-dev-ncneworkflowdatabase].[dbo].[TaskStageComment] tsc
JOIN [taskmanager-dev-ncneworkflowdatabase].[dbo].[TaskStage] ts ON ts.TaskStageId = tsc.TaskStageId
WHERE ts.TaskStageTypeId = 21

-- Now remove all TaskStages that use 'Consider Email to SDR' TaskStageType
BEGIN TRAN t1
DELETE FROM [taskmanager-dev-ncneworkflowdatabase].[dbo].[TaskStage]
WHERE [TaskStageTypeId] = 21
 
-- Verify that the TaskStages that use 'Consider Email to SDR' TaskStageType have been deleted
-- There should be zero rows returned
SELECT * 
FROM [taskmanager-dev-ncneworkflowdatabase].[dbo].[TaskStage]
WHERE [TaskStageTypeId] = 21

-- Now remove 'Consider Email to SDR' TaskStageType
-- Should be 1 row affected
DELETE FROM [taskmanager-dev-ncneworkflowdatabase].[dbo].[TaskStageType]
WHERE [TaskStageTypeId] = 21
 
-- Verify that the 'Consider Email to SDR' TaskStageType has been deleted
-- There should be zero rows returned
SELECT * 
FROM [taskmanager-dev-ncneworkflowdatabase].[dbo].[TaskStageType]
WHERE [TaskStageTypeId] = 21

-- Commit transaction
COMMIT TRAN t1