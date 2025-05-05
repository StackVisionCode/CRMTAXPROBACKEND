using AutoMapper;
using Domains.Requirements;
using DTOs.StatusRequiremtDto;


namespace Profiles
{
    public class StatusRequirementProfile : Profile
    {
        public StatusRequirementProfile()
        {
            CreateMap<CreateStatusRequirementDto, StatusRequirement>();
            CreateMap<UpdateStatusRequirementDto, StatusRequirement>();
            CreateMap<StatusRequirement, ReadRequiremenStatustDtos>();
        }
    }
}
