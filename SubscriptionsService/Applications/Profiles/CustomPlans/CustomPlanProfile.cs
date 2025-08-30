using AutoMapper;
using Commands.CustomPlanCommands;
using Domains;
using DTOs.CustomPlanDTOs;

namespace AuthService.Profiles.CustomPlans;

public class CustomPlanProfile : Profile
{
    public CustomPlanProfile()
    {
        // Basic mappings
        CreateMap<NewCustomPlanDTO, CustomPlan>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CustomModules, o => o.Ignore());

        CreateMap<UpdateCustomPlanDTO, CustomPlan>()
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.CustomModules, o => o.Ignore());

        CreateMap<CustomPlan, CustomPlanDTO>()
            .ForMember(
                d => d.AdditionalModuleNames,
                o =>
                    o.MapFrom(s =>
                        s.CustomModules.Where(cm => cm.IsIncluded).Select(cm => cm.Module.Name)
                    )
            );

        // Command mappings
        CreateMap<NewCustomPlanDTO, CreateCustomPlanCommand>()
            .ConstructUsing(src => new CreateCustomPlanCommand(src));

        CreateMap<UpdateCustomPlanDTO, UpdateCustomPlanCommand>()
            .ConstructUsing(src => new UpdateCustomPlanCommand(src));
    }
}
