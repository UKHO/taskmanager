using AutoMapper;
using Portal.Models;
using WorkflowDatabase.EF.Models;

namespace Portal.MappingProfiles
{
    public class HistoricalTasksDataMappingProfile : Profile
    {
        public HistoricalTasksDataMappingProfile()
        {
            CreateMap<WorkflowInstance, HistoricalTasksData>()
                .ForMember(destination => destination.TaskStage,
                    opts => opts.MapFrom(source => source.ActivityName))
                .ForMember(destination => destination.RsdraNumber,
                    opts => opts.MapFrom(source => source.AssessmentData.RsdraNumber))
                .ForMember(destination => destination.SourceDocumentName,
                    opts => opts.MapFrom(source => source.AssessmentData.SourceDocumentName))
                .ForMember(destination => destination.Team,
                    opts => opts.MapFrom(source => source.AssessmentData.TeamDistributedTo));

        }
    }
}
