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
ad.SourceDocumentType as [SOURCE CATEGORY],
case 
	when wi.ActivityName = reviewStage then ISNULL(dard.TaskType, '')
	when wi.ActivityName = assessStage then ISNULL(daad.TaskType, '')
	when wi.ActivityName = verifyStage then ISNULL(davd.TaskType, '')
end as [TASK TYPE],
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
ISNULL(dard.WorkspaceAffected, '') as [Chart Affected],
ISNULL(tn.[Text], '') as COMMENTS,
case 
	when oh.OnHoldTime is not null then 'YES' 
	else '' 
end as [ON HOLD],
ISNULL(CONVERT(nvarchar(20), oh.OnHoldTime, 103), '') as [ON HOLD START],
ISNULL(CONVERT(nvarchar(20), oh.OffHoldTime, 103), '') as [ON HOLD END],
ISNULL(DATEDIFF(dd, oh.OnHoldTime, oh.OffHoldTime), '') as [DAYS ON HOLD],
ISNULL(ad.TeamDistributedTo, '') as [HW OR PR],
case 
	when wi.ActivityName = assessStage then 'Compilation'
	when wi.ActivityName = verifyStage then 'Verification' 
	else wi.[ActivityName]
end as [TASK STAGE],
wi.[Status] as [Status],
case 
	when wi.ActivityName = reviewStage then ISNULL(dardAssessorUser.DisplayName, '')
	when wi.ActivityName = assessStage then ISNULL(daadAssessorUser.DisplayName, '')
	when wi.ActivityName = verifyStage then ISNULL(davdAssessorUser.DisplayName, '')
end as [DB COMPILER],
'Unknown' as [COMP TIME],
case 
	when wi.ActivityName = reviewStage then ISNULL(dardVerifierUser.DisplayName, '')
	when wi.ActivityName = assessStage then ISNULL(daadVerifierUser.DisplayName, '')
	when wi.ActivityName = verifyStage then ISNULL(davdVerifierUser.DisplayName, '')
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
			and OffHoldTime is null) oh
on wi.ProcessId = oh.ProcessId

left join dbo.AdUsers dardAssessorUser on dard.AssessorAdUserId = dardAssessorUser.AdUserId
left join dbo.AdUsers daadAssessorUser on daad.AssessorAdUserId = daadAssessorUser.AdUserId
left join dbo.AdUsers davdAssessorUser on davd.AssessorAdUserId = davdAssessorUser.AdUserId
left join dbo.AdUsers dardVerifierUser on dard.VerifierAdUserId = dardVerifierUser.AdUserId
left join dbo.AdUsers daadVerifierUser on daad.VerifierAdUserId = daadVerifierUser.AdUserId
left join dbo.AdUsers davdVerifierUser on davd.VerifierAdUserId = davdVerifierUser.AdUserId