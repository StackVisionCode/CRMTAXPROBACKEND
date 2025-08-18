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

        CreateMap<CustomPlan, CustomPlanWithStatsDTO>()
            .IncludeBase<CustomPlan, CustomPlanDTO>()
            // Los campos estadísticos se calculan en el handler
            .ForMember(d => d.TotalUsers, o => o.Ignore())
            .ForMember(d => d.ActiveUsers, o => o.Ignore())
            .ForMember(d => d.OwnerCount, o => o.Ignore())
            .ForMember(d => d.RegularUsersCount, o => o.Ignore())
            .ForMember(d => d.BaseServiceName, o => o.Ignore())
            .ForMember(d => d.BaseServiceTitle, o => o.Ignore())
            .ForMember(d => d.BaseServiceFeatures, o => o.Ignore())
            .ForMember(d => d.ServiceUserLimit, o => o.Ignore())
            .ForMember(d => d.IsWithinLimits, o => o.Ignore())
            .ForMember(d => d.BaseModuleNames, o => o.Ignore())
            .ForMember(d => d.ExtraModuleNames, o => o.Ignore())
            .ForMember(d => d.RevenuePerUser, o => o.Ignore())
            .ForMember(d => d.ModuleUtilization, o => o.Ignore())
            .ForMember(d => d.TotalModules, o => o.Ignore())
            .ForMember(d => d.ActiveModules, o => o.Ignore())
            .ForMember(d => d.IsExpired, o => o.Ignore())
            .ForMember(d => d.DaysUntilExpiry, o => o.Ignore())
            .ForMember(d => d.MonthlyRevenue, o => o.Ignore());

        // Command mappings
        CreateMap<NewCustomPlanDTO, CreateCustomPlanCommand>()
            .ConstructUsing(src => new CreateCustomPlanCommand(src));

        CreateMap<UpdateCustomPlanDTO, UpdateCustomPlanCommand>()
            .ConstructUsing(src => new UpdateCustomPlanCommand(src));
    }
}
