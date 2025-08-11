using AuthService.Domains.CustomPlans;
using AuthService.DTOs.CustomPlanDTOs;
using AutoMapper;
using Commands.CustomPlanCommands;

namespace AuthService.Profiles.CustomPlans;

public class CustomPlanProfile : Profile
{
    public CustomPlanProfile()
    {
        // Basic mappings
        CreateMap<NewCustomPlanDTO, CustomPlan>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Company, o => o.Ignore())
            .ForMember(d => d.CustomModules, o => o.Ignore());

        CreateMap<UpdateCustomPlanDTO, CustomPlan>()
            .ForMember(d => d.CompanyId, o => o.Ignore()) // No se puede cambiar
            .ForMember(d => d.Company, o => o.Ignore())
            .ForMember(d => d.CustomModules, o => o.Ignore());

        CreateMap<CustomPlan, CustomPlanDTO>()
            .ForMember(
                d => d.CompanyName,
                o =>
                    o.MapFrom(s => s.Company.IsCompany ? s.Company.CompanyName : s.Company.FullName)
            )
            .ForMember(d => d.CompanyDomain, o => o.MapFrom(s => s.Company.Domain))
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
