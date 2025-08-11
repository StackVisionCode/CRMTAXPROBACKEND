using AuthService.Domains.Services;
using AuthService.DTOs.ServiceDTOs;
using AutoMapper;
using Commands.ServiceCommands;

namespace AuthService.Profiles.Services;

public class ServiceProfile : Profile
{
    public ServiceProfile()
    {
        // Basic mappings
        CreateMap<NewServiceDTO, Service>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Modules, o => o.Ignore()) // Módulos se asignan por separado
            .ForMember(d => d.Companies, o => o.Ignore());

        CreateMap<UpdateServiceDTO, Service>()
            .ForMember(d => d.Modules, o => o.Ignore())
            .ForMember(d => d.Companies, o => o.Ignore());

        CreateMap<Service, ServiceDTO>()
            .ForMember(d => d.ModuleNames, o => o.MapFrom(s => s.Modules.Select(m => m.Name)))
            .ForMember(d => d.ModuleIds, o => o.MapFrom(s => s.Modules.Select(m => m.Id)));

        // Command mappings
        CreateMap<NewServiceDTO, CreateServiceCommand>()
            .ConstructUsing(src => new CreateServiceCommand(src));

        CreateMap<UpdateServiceDTO, UpdateServiceCommand>()
            .ConstructUsing(src => new UpdateServiceCommand(src));
    }
}
