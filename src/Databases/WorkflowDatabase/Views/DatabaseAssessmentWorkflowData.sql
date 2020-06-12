CREATE VIEW [dbo].[DatabaseAssessmentWorkflowData]

AS

WITH params AS (
	SELECT 
	simpleTaskType='Simple', 
	reviewStage='Review',
	assessStage='Assess',
	verifyStage='Verify',
	completedStatus='Completed')

SELECT
ad.RsdraNumber as [RSDRA No],
ad.PrimarySdocId as [SDOC ID],
ad.SourceDocumentName as [SOURCE NAME],
ad.SourceDocumentType as [SOURCE TYPE],
ISNULL(CONVERT(nvarchar(20), ad.ReceiptDate, 103), '') as [RECEIPT DATE],
ISNULL(CONVERT(nvarchar(20), ad.ToSdoDate, 103), '') as [TO SDO DATE],
ISNULL(CONVERT(nvarchar(20), wi.StartedAt, 103), '') as [DM RECEIPT],
case
	when ad.EffectiveStartDate is not null then 
		case 
			when wi.ActivityName = reviewStage or dard.TaskType = simpleTaskType
				then CONVERT(nvarchar(20), DATEADD(dd, 14, ad.EffectiveStartDate), 103)
			else CONVERT(nvarchar(20), DATEADD(dd, 72, ad.EffectiveStartDate), 103)
		end
	else ''
end as [DM END DATE],
case
	when ad.EffectiveStartDate is not null then 
		case 
			when wi.ActivityName = reviewStage or dard.TaskType = simpleTaskType
				then DATEDIFF(dd, GETDATE(), DATEADD(dd, 14, ad.EffectiveStartDate)) 
			else DATEDIFF(dd, GETDATE(), DATEADD(dd, 72, ad.EffectiveStartDate))
		end
	else ''
end as [DAYS TO DM END],
case
	when ad.EffectiveStartDate is not null then CONVERT(nvarchar(20), DATEADD(DD, 20, ad.EffectiveStartDate), 103)
	else ''
end as [EXTERNAL DM END],
case
	when ad.EffectiveStartDate is not null then DATEDIFF(dd, GETDATE(), DATEADD(DD, 20, ad.EffectiveStartDate))
	else ''
end as [EXT DAYS TO DM END],
dard.WorkspaceAffected as [Chart Affected],
ISNULL(tn.[Text], '') as COMMENTS,
case 
	when oh.OffHoldTime is null then 'YES' 
	else '' 
end as [ON HOLD],
ISNULL(CONVERT(nvarchar(20), oh.OnHoldTime, 103), '') as [ON HOLD START],
ISNULL(CONVERT(nvarchar(20), oh.OffHoldTime, 103), '') as [ON HOLD END],
ISNULL(DATEDIFF(dd, oh.OnHoldTime, oh.OffHoldTime), '') as [DAYS ON HOLD],
ISNULL(ad.TeamDistributedTo, '') as [HW OR PR],
case 
	when wi.ActivityName = assessStage then 'Compilation'
	when wi.ActivityName = verifyStage then 'Verification' 
	else ''
end as [TASK STAGE],
case 
	when wi.ActivityName = reviewStage then ISNULL(dard.Assessor, '')
	when wi.ActivityName = assessStage then ISNULL(daad.Assessor, '')
	when wi.ActivityName = verifyStage then ISNULL(davd.Assessor, '')
end as [DB COMPILER],
'Unknown' as [COMP TIME],
case 
	when wi.ActivityName = reviewStage then ISNULL(dard.Verifier, '')
	when wi.ActivityName = assessStage then ISNULL(daad.Verifier, '')
	when wi.ActivityName = verifyStage then ISNULL(davd.Verifier, '')
end as [DB VERIFIER],
'Unknown' as [VERIF TIME],
case
	when wi.[Status] = completedStatus then DATEDIFF(dd, wi.StartedAt, wi.ActivityChangedAt)
	else ''
end as [TOTAL TIME],
case 
	when wi.[Status] = completedStatus then CONVERT(nvarchar(20), wi.ActivityChangedAt, 103)
	else ''
end as [DM SIGNOFF],
case
	when wi.[Status] = completedStatus then DATEDIFF(dd, wi.StartedAt, wi.ActivityChangedAt)
	else ''
end as [DM DAYS],
wi.StartedAt as [LOAD DATE]

from dbo.WorkflowInstance wi
join params ON 1=1
left join dbo.AssessmentData ad on wi.ProcessId = ad.ProcessId
left join dbo.TaskNote tn on wi.ProcessId = tn.ProcessId
join dbo.DbAssessmentReviewData dard on wi.ProcessId = dard.ProcessId
left join dbo.DbAssessmentAssessData daad on wi.ProcessId = daad.ProcessId
left join dbo.DbAssessmentVerifyData davd on wi.ProcessId = davd.ProcessId
left join (select top (1) * from dbo.OnHold 
			where OnHoldTime is not null 
			and OffHoldTime is not null) oh
on wi.ProcessId = oh.ProcessId