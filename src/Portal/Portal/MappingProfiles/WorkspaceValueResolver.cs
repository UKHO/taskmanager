using AutoMapper;
using Portal.ViewModels;
using WorkflowDatabase.EF.Models;

namespace Portal.MappingProfiles
{
    public class WorkspaceValueResolver : IValueResolver<WorkflowInstance, TaskViewModel, string>
    {
        public string Resolve(WorkflowInstance source, TaskViewModel destination, string destMember, ResolutionContext context)
        {
            switch (source.ActivityName)
            {
                case "Review":
                    return source.DbAssessmentReviewData != null ? source.DbAssessmentReviewData.WorkspaceAffected : string.Empty;
                case "Assess":
                    return source.DbAssessmentAssessData != null ? source.DbAssessmentAssessData.WorkspaceAffected : string.Empty;
                case "Verify":
                    return source.DbAssessmentVerifyData != null ? source.DbAssessmentVerifyData.WorkspaceAffected : string.Empty;
                default:
                    return string.Empty;
            }
        }
    }
}
