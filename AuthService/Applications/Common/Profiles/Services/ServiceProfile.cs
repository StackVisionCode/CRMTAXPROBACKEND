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
            .ForMember(d => d.Modules, o => o.Ignore()); // Módulos se asignan por separado

        CreateMap<UpdateServiceDTO, Service>().ForMember(d => d.Modules, o => o.Ignore());

        CreateMap<Service, ServiceDTO>()
            .ForMember(d => d.ModuleNames, o => o.MapFrom(s => s.Modules.Select(m => m.Name)))
            .ForMember(d => d.ModuleIds, o => o.MapFrom(s => s.Modules.Select(m => m.Id)));

        CreateMap<Service, ServiceWithStatsDTO>()
            .ForMember(d => d.ModuleNames, o => o.MapFrom(s => s.Modules.Select(m => m.Name)))
            .ForMember(d => d.ModuleIds, o => o.MapFrom(s => s.Modules.Select(m => m.Id)))
            // Las estadísticas se calculan en el handler
            .ForMember(d => d.CompaniesUsingCount, o => o.Ignore())
            .ForMember(d => d.TotalActiveUsers, o => o.Ignore())
            .ForMember(d => d.TotalRevenue, o => o.Ignore())
            .ForMember(d => d.TotalOwnersUsing, o => o.Ignore())
            .ForMember(d => d.TotalRegularUsersUsing, o => o.Ignore())
            .ForMember(d => d.AverageRevenuePerCompany, o => o.Ignore())
            .ForMember(d => d.AverageUsersPerCompany, o => o.Ignore())
            .ForMember(d => d.RevenuePerUser, o => o.Ignore());

        // Command mappings
        CreateMap<NewServiceDTO, CreateServiceCommand>()
            .ConstructUsing(src => new CreateServiceCommand(src));

        CreateMap<UpdateServiceDTO, UpdateServiceCommand>()
            .ConstructUsing(src => new UpdateServiceCommand(src));
    }
}
