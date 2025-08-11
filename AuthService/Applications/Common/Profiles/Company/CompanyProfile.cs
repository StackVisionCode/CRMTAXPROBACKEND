using AuthService.Applications.DTOs.CompanyDTOs;
using AuthService.Domains.Companies;
using AutoMapper;
using Commands.UserCommands;

namespace AuthService.Profiles.User;

public class CompanyProfile : Profile
{
    public CompanyProfile()
    {
        CreateMap<NewCompanyDTO, Company>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.AddressId, o => o.Ignore())
            .ForMember(d => d.Address, o => o.Ignore())
            .ForMember(d => d.CustomPlanId, o => o.Ignore()) // Se crea en el handler
            .ForMember(d => d.CustomPlan, o => o.Ignore())
            .ForMember(d => d.TaxUsers, o => o.Ignore())
            .ForMember(d => d.UserCompanies, o => o.Ignore());

        CreateMap<UpdateCompanyDTO, Company>()
            .ForMember(d => d.AddressId, o => o.Ignore())
            .ForMember(d => d.Address, o => o.Ignore())
            .ForMember(d => d.CustomPlanId, o => o.Ignore()) // No se puede cambiar directamente
            .ForMember(d => d.CustomPlan, o => o.Ignore())
            .ForMember(d => d.TaxUsers, o => o.Ignore())
            .ForMember(d => d.UserCompanies, o => o.Ignore());

        CreateMap<Company, CompanyDTO>()
            .ForMember(d => d.Address, o => o.MapFrom(s => s.Address))
            // Información del CustomPlan
            .ForMember(d => d.CustomPlanPrice, o => o.MapFrom(s => s.CustomPlan.Price))
            .ForMember(d => d.CustomPlanIsActive, o => o.MapFrom(s => s.CustomPlan.IsActive))
            .ForMember(d => d.CustomPlanStartDate, o => o.MapFrom(s => s.CustomPlan.StartDate))
            .ForMember(d => d.CustomPlanEndDate, o => o.MapFrom(s => s.CustomPlan.EndDate))
            .ForMember(d => d.CustomPlanRenewDate, o => o.MapFrom(s => s.CustomPlan.RenewDate))
            .ForMember(d => d.CustomPlanIsRenewed, o => o.MapFrom(s => s.CustomPlan.isRenewed))
            // Contadores de usuarios
            .ForMember(d => d.CurrentTaxUserCount, o => o.MapFrom(s => s.TaxUsers.Count()))
            .ForMember(d => d.CurrentUserCompanyCount, o => o.MapFrom(s => s.UserCompanies.Count()))
            // Módulos adicionales del CustomPlan
            .ForMember(
                d => d.BaseServiceName,
                o =>
                    o.MapFrom(s =>
                        s.CustomPlan.CustomModules.Where(cm => cm.Module.ServiceId != null)
                            .Select(cm => cm.Module.Service != null ? cm.Module.Service.Name : null)
                            .FirstOrDefault()
                    )
            )
            .ForMember(
                d => d.BaseModules,
                o =>
                    o.MapFrom(s =>
                        s.CustomPlan.CustomModules.Where(cm =>
                                cm.IsIncluded && cm.Module.ServiceId != null
                            )
                            .Select(cm => cm.Module.Name)
                    )
            )
            .ForMember(
                d => d.AdditionalModules,
                o =>
                    o.MapFrom(s =>
                        s.CustomPlan.CustomModules.Where(cm =>
                                cm.IsIncluded && cm.Module.ServiceId == null
                            )
                            .Select(cm => cm.Module.Name)
                    )
            );

        // Command mappings - ACTUALIZADO
        CreateMap<NewCompanyDTO, CreateTaxCompanyCommands>()
            .ConstructUsing(src => new CreateTaxCompanyCommands(src, string.Empty));

        CreateMap<UpdateCompanyDTO, UpdateTaxCompanyCommands>()
            .ConstructUsing(src => new UpdateTaxCompanyCommands(src));
    }
}
