using AutoMapper;
using DataServices.Models;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.MappingProfiles
{
    public class AssessmentDataMappingProfile : Profile
    {
        public AssessmentDataMappingProfile()
        {
            CreateMap<DocumentAssessmentData, AssessmentData>()
                .ForMember(destination => destination.SourceDocumentName,
                    opts => opts.MapFrom(source => source.Name))
                .ForMember(destination => destination.ToSdoDate,
                    opts => opts.MapFrom(source => source.SDODate))
                .ForMember(destination => destination.TeamDistributedTo,
                    opts => opts.MapFrom(source => source.Team))
                .ForMember(destination => destination.SourceDocumentType,
                    opts => opts.MapFrom(source => source.DocumentType))
                .ForMember(destination => destination.SourceNature,
                    opts => opts.MapFrom(source => source.DocumentNature))
                .ForMember(destination => destination.RsdraNumber,
                    opts => opts.MapFrom(source => source.SourceName));

            CreateMap<DocumentAssessmentData, Comment>()
                .ForMember(destination => destination.Text,
                    opts => opts.MapFrom(source => source.Notes));
        }
    }
}
