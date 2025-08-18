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
            .ForMember(d => d.CustomPlanId, o => o.Ignore())
            .ForMember(d => d.CustomPlan, o => o.Ignore())
            .ForMember(d => d.TaxUsers, o => o.Ignore());

        CreateMap<UpdateCompanyDTO, Company>()
            .ForMember(d => d.AddressId, o => o.Ignore())
            .ForMember(d => d.Address, o => o.Ignore())
            .ForMember(d => d.CustomPlanId, o => o.Ignore())
            .ForMember(d => d.CustomPlan, o => o.Ignore())
            .ForMember(d => d.TaxUsers, o => o.Ignore());

        CreateMap<Company, CompanyDTO>()
            .ForMember(d => d.Address, o => o.MapFrom(s => s.Address))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt))
            // Información del CustomPlan
            .ForMember(d => d.CustomPlanPrice, o => o.MapFrom(s => s.CustomPlan.Price))
            .ForMember(d => d.CustomPlanUserLimit, o => o.MapFrom(s => s.CustomPlan.UserLimit))
            .ForMember(d => d.CustomPlanIsActive, o => o.MapFrom(s => s.CustomPlan.IsActive))
            .ForMember(d => d.CustomPlanStartDate, o => o.MapFrom(s => s.CustomPlan.StartDate))
            .ForMember(d => d.CustomPlanRenewDate, o => o.MapFrom(s => s.CustomPlan.RenewDate))
            .ForMember(d => d.CustomPlanIsRenewed, o => o.MapFrom(s => s.CustomPlan.isRenewed))
            // Contadores de usuarios actualizados
            .ForMember(d => d.CurrentTaxUserCount, o => o.MapFrom(s => s.TaxUsers.Count()))
            // Módulos del CustomPlan
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
            )
            .ForMember(
                d => d.BaseServiceTitle,
                o =>
                    o.MapFrom(s =>
                        s.CustomPlan.CustomModules.Where(cm => cm.Module.ServiceId != null)
                            .Select(cm =>
                                cm.Module.Service != null ? cm.Module.Service.Title : null
                            )
                            .FirstOrDefault()
                    )
            )
            .ForMember(
                d => d.BaseServiceFeatures,
                o =>
                    o.MapFrom(s =>
                        s.CustomPlan.CustomModules.Where(cm => cm.Module.ServiceId != null)
                            .Select(cm =>
                                cm.Module.Service != null
                                    ? cm.Module.Service.Features
                                    : new List<string>()
                            )
                            .FirstOrDefault() ?? new List<string>()
                    )
            )
            // Info del TaxUser Owner
            .ForMember(
                d => d.AdminUserId,
                o =>
                    o.MapFrom(s =>
                        s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault() != null
                            ? s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault()!.Id
                            : Guid.Empty
                    )
            )
            .ForMember(
                d => d.AdminEmail,
                o =>
                    o.MapFrom(s =>
                        s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault() != null
                            ? s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault()!.Email
                            : string.Empty
                    )
            )
            .ForMember(
                d => d.AdminName,
                o =>
                    o.MapFrom(s =>
                        s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault() != null
                            ? s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault()!.Name
                            : null
                    )
            )
            .ForMember(
                d => d.AdminLastName,
                o =>
                    o.MapFrom(s =>
                        s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault() != null
                            ? s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault()!.LastName
                            : null
                    )
            )
            .ForMember(
                d => d.AdminPhoneNumber,
                o =>
                    o.MapFrom(s =>
                        s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault() != null
                            ? s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault()!.PhoneNumber
                            : null
                    )
            )
            .ForMember(
                d => d.AdminPhotoUrl,
                o =>
                    o.MapFrom(s =>
                        s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault() != null
                            ? s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault()!.PhotoUrl
                            : null
                    )
            )
            .ForMember(
                d => d.AdminIsActive,
                o =>
                    o.MapFrom(s =>
                        s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault() != null
                            ? s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault()!.IsActive
                            : false
                    )
            )
            .ForMember(
                d => d.AdminConfirmed,
                o =>
                    o.MapFrom(s =>
                        s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault() != null
                            ? (s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault()!.Confirm ?? false)
                            : false
                    )
            )
            .ForMember(
                d => d.AdminAddress,
                o =>
                    o.MapFrom(s =>
                        s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault() != null
                            ? s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault()!.Address
                            : null
                    )
            )
            .ForMember(
                d => d.AdminRoleNames,
                o =>
                    o.MapFrom(s =>
                        s.TaxUsers.Where(u => u.IsOwner).FirstOrDefault() != null
                            ? s
                                .TaxUsers.Where(u => u.IsOwner)
                                .FirstOrDefault()!
                                .UserRoles.Select(ur => ur.Role.Name)
                            : new List<string>()
                    )
            );

        // Command mappings
        CreateMap<NewCompanyDTO, CreateTaxCompanyCommands>()
            .ConstructUsing(src => new CreateTaxCompanyCommands(src, string.Empty));

        CreateMap<UpdateCompanyDTO, UpdateTaxCompanyCommands>()
            .ConstructUsing(src => new UpdateTaxCompanyCommands(src));
    }
}
