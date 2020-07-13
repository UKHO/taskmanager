CREATE VIEW [dbo].[NewChartNewEditionWorkflowData]

AS

WITH params AS (
	SELECT 
	compTaskType=comp.TaskStageTypeId, 
	v1TaskType=v1.TaskStageTypeId, 
	v2TaskType=v2.TaskStageTypeId, 
	hundredTaskType=hund.TaskStageTypeId,
	formsTaskType=forms.TaskStageTypeId,
	specTaskType=spec.TaskStageTypeId,
	v1ReworkTaskType = v1Rework.TaskStageTypeId,
	v2ReworkTaskType = v2Rework.TaskStageTypeId,
	commitPrintTaskType = commitPrint.TaskStageTypeId
	from [dbo].[TaskStageType] comp 
	join [dbo].[TaskStageType] v1 on 1=1 and v1.[Name] = 'V1'
	join [dbo].[TaskStageType] v2 on 1=1 and v2.[Name] = 'V2'
	join [dbo].[TaskStageType] hund on 1=1 and hund.[Name] = '100% Check'
	join [dbo].[TaskStageType] forms on 1=1 and forms.[Name] = 'Forms'
	join [dbo].[TaskStageType] spec on 1=1 and spec.[Name] = 'Specification'
	join [dbo].[TaskStageType] v1Rework on 1=1 and v1Rework.[Name] = 'V1 Rework'
	join [dbo].[TaskStageType] v2Rework on 1=1 and v2Rework.[Name] = 'V2 Rework'
	join [dbo].[TaskStageType] commitPrint on 1=1 and commitPrint.[Name] = 'Commit to Print'
	where comp.[Name] = 'Compile Chart')

SELECT
ISNULL(ti.ChartNumber, '') as [Chart No.],
ISNULL(ti.Country, '') as [Location],
ti.WorkflowType as [Type],
'?' as [Sales],
CASE
	WHEN ti.ChartType = 'Primary' THEN 'Yes' 
	ELSE 'No'
END as [HDT],
'?' as [DCPT],
'?' as [PPT],
CASE
	WHEN ti.ChartType = 'Thematics' 
	AND SUBSTRING(ti.ChartNumber, 1, 1) = '8'
	AND LEN(ti.ChartNumber) = 4
	THEN 'Yes' 
	ELSE 'No'
END as [PAG],
CASE 
	WHEN spec.Status = 'Completed' THEN 'Yes'
	ELSE 'No'
END as [Spec],
ISNULL(ti.Ion, '') as [ION],
'?' as [Specifier],
'?' as [Complexity],
CASE 
	WHEN spec.Status = 'Completed' THEN 'Yes'
	ELSE 'No'
END as [Started],
isnull(comp.AssignedUser, '') as [Comp],
isnull(v1.AssignedUser, '') as [V1],
isnull(v1Rework.AssignedUser, '') as [V1 Corr],
isnull(v2.AssignedUser, '') as [V2],
isnull(v2Rework.AssignedUser, '') as [V2 Corr],
isnull(comp.AssignedUser, '') as [Circ],
isnull(CONVERT(varchar(20), forms.DateCompleted, 103), '') as [100%],
isnull(CONVERT(varchar(20), forms.DateExpected, 103), '') as [H Forms],
isnull(CONVERT(varchar(20), commitPrint.DateExpected, 103), '') as [CTP],
isnull(CONVERT(varchar(20), commitPrint.DateExpected, 103), '') as [2Wk],
isnull(CONVERT(varchar(20), ti.CISDate, 103), '') as [CIS],
isnull(CONVERT(varchar(20), ti.PublicationDate, 103), '') as [Pub],
'?' as [Withdraw],
'?' as [IIC],
isnull(tn.[Text], '') as Comments

from [dbo].[TaskInfo] ti
join params on 1=1
join [dbo].[TaskStage] comp on ti.ProcessId = comp.ProcessId and comp.TaskStageTypeId = params.compTaskType
join [dbo].[TaskStage] v1 on ti.ProcessId = v1.ProcessId and v1.TaskStageTypeId = params.v1TaskType
join [dbo].[TaskStage] v2 on ti.ProcessId = v2.ProcessId and v2.TaskStageTypeId = params.v2TaskType
join [dbo].[TaskStage] hund on ti.ProcessId = hund.ProcessId and hund.TaskStageTypeId = params.hundredTaskType
join [dbo].[TaskStage] forms on ti.ProcessId = forms.ProcessId and forms.TaskStageTypeId = params.formsTaskType
join [dbo].[TaskStage] spec on ti.ProcessId = spec.ProcessId and spec.TaskStageTypeId = params.specTaskType
join [dbo].[TaskStage] v1Rework on ti.ProcessId = v1Rework.ProcessId and v1Rework.TaskStageTypeId = params.v1ReworkTaskType
join [dbo].[TaskStage] v2Rework on ti.ProcessId = v2Rework.ProcessId and v2Rework.TaskStageTypeId = params.v2ReworkTaskType
join [dbo].[TaskStage] commitPrint on ti.ProcessId = commitPrint.ProcessId and commitPrint.TaskStageTypeId = params.commitPrintTaskType
left join [dbo].[TaskNote] tn on ti.ProcessId = tn.ProcessId