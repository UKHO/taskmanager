using AutoMapper;
using Portal.ViewModels;
using WorkflowDatabase.EF.Models;

namespace Portal.MappingProfiles
{
    public class TaskViewModelMappingProfile : Profile
    {
        public TaskViewModelMappingProfile()
        {
            CreateMap<WorkflowInstance, TaskViewModel>()
                .ForMember(destination => destination.TaskStage,
                    opts => opts.MapFrom(source => source.ActivityName))
                .ForMember(destination => destination.Team,
                    opts => opts.MapFrom(source => source.AssessmentData.TeamDistributedTo))
                .ForMember(destination => destination.Workspace,
                    opts => opts.MapFrom<WorkspaceValueResolver>());

            CreateMap<TaskNote, TaskViewModel>().ForMember(destination => destination.TaskNoteText,
                opts=>opts.MapFrom(source=>source.Text));
        }
    }
}
