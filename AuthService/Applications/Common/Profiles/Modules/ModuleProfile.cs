using AuthService.Domains.Modules;
using AuthService.DTOs.ModuleDTOs;
using AutoMapper;
using Commands.ModuleCommands;

namespace AuthService.Profiles.Modules;

public class ModuleProfile : Profile
{
    public ModuleProfile()
    {
        CreateMap<NewModuleDTO, Module>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Service, o => o.Ignore())
            .ForMember(d => d.CustomModules, o => o.Ignore());

        CreateMap<UpdateModuleDTO, Module>()
            .ForMember(d => d.Service, o => o.Ignore())
            .ForMember(d => d.CustomModules, o => o.Ignore());

        CreateMap<Module, ModuleDTO>()
            .ForMember(
                d => d.ServiceName,
                o => o.MapFrom(s => s.Service != null ? s.Service.Name : null)
            );

        // Command mappings
        CreateMap<NewModuleDTO, CreateModuleCommand>()
            .ConstructUsing(src => new CreateModuleCommand(src));

        CreateMap<UpdateModuleDTO, UpdateModuleCommand>()
            .ConstructUsing(src => new UpdateModuleCommand(src));

        CreateMap<AssignModuleToServiceDTO, AssignModuleToServiceCommand>()
            .ConstructUsing(src => new AssignModuleToServiceCommand(src.ModuleId, src.ServiceId));
    }
}
