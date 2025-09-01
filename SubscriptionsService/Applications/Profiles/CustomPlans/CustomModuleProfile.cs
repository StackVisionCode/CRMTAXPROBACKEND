using AutoMapper;
using Commands.CustomModuleCommands;
using Domains;
using DTOs.CustomModuleDTOs;

namespace AuthService.Profiles.CustomPlans;

public class CustomModuleProfile : Profile
{
    public CustomModuleProfile()
    {
        CreateMap<NewCustomModuleDTO, CustomModule>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CustomPlanId, o => o.Ignore()) // Se asigna en el handler
            .ForMember(d => d.CustomPlan, o => o.Ignore())
            .ForMember(d => d.Module, o => o.Ignore());

        CreateMap<UpdateCustomModuleDTO, CustomModule>()
            .ForMember(d => d.CustomPlanId, o => o.Ignore())
            .ForMember(d => d.ModuleId, o => o.Ignore())
            .ForMember(d => d.CustomPlan, o => o.Ignore())
            .ForMember(d => d.Module, o => o.Ignore());

        CreateMap<CustomModule, CustomModuleDTO>()
            .ForMember(d => d.ModuleName, o => o.MapFrom(s => s.Module.Name))
            .ForMember(d => d.ModuleDescription, o => o.MapFrom(s => s.Module.Description))
            .ForMember(d => d.ModuleUrl, o => o.MapFrom(s => s.Module.Url));

        // Command mappings
        CreateMap<AssignCustomModuleDTO, AssignCustomModuleCommand>()
            .ConstructUsing(src => new AssignCustomModuleCommand(src));

        CreateMap<UpdateCustomModuleDTO, UpdateCustomModuleCommand>()
            .ConstructUsing(src => new UpdateCustomModuleCommand(src));
    }
}
