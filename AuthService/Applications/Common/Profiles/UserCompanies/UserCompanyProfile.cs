using Applications.DTOs.CompanyDTOs;
using AuthService.Domains.UserCompanies;
using AuthService.DTOs.UserCompanyDTOs;
using AutoMapper;
using Commands.UserCompanyCommands;

namespace AuthService.Profiles.UserCompanies;

public class UserCompanyProfile : Profile
{
    public UserCompanyProfile()
    {
        // Basic mappings
        CreateMap<NewUserCompanyDTO, UserCompany>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Password, o => o.Ignore()) // Se hashea en el handler
            .ForMember(d => d.AddressId, o => o.Ignore())
            .ForMember(d => d.Address, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(s => false)) // Inactivo hasta confirmar
            .ForMember(d => d.Confirm, o => o.MapFrom(s => false))
            .ForMember(d => d.OtpVerified, o => o.MapFrom(s => false))
            .ForMember(d => d.Company, o => o.Ignore())
            .ForMember(d => d.UserCompanyRoles, o => o.Ignore())
            .ForMember(d => d.UserCompanySessions, o => o.Ignore())
            .ForMember(d => d.CompanyPermissions, o => o.Ignore());

        CreateMap<UpdateUserCompanyDTO, UserCompany>()
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.Email, o => o.Ignore())
            .ForMember(d => d.Password, o => o.Ignore())
            .ForMember(d => d.AddressId, o => o.Ignore())
            .ForMember(d => d.Address, o => o.Ignore())
            .ForMember(d => d.Company, o => o.Ignore())
            .ForMember(d => d.UserCompanyRoles, o => o.Ignore())
            .ForMember(d => d.UserCompanySessions, o => o.Ignore())
            .ForMember(d => d.CompanyPermissions, o => o.Ignore());

        CreateMap<UserCompany, UserCompanyDTO>()
            .ForMember(d => d.Address, o => o.MapFrom(s => s.Address))
            .ForMember(d => d.CompanyFullName, o => o.MapFrom(s => s.Company.FullName))
            .ForMember(d => d.CompanyName, o => o.MapFrom(s => s.Company.CompanyName))
            .ForMember(d => d.CompanyBrand, o => o.MapFrom(s => s.Company.Brand))
            .ForMember(d => d.CompanyIsIndividual, o => o.MapFrom(s => !s.Company.IsCompany))
            .ForMember(d => d.CompanyDomain, o => o.MapFrom(s => s.Company.Domain))
            .ForMember(d => d.CompanyAddress, o => o.MapFrom(s => s.Company.Address))
            .ForMember(
                d => d.RoleNames,
                o => o.MapFrom(s => s.UserCompanyRoles.Select(ucr => ucr.Role.Name))
            )
            .ForMember(
                d => d.CustomPermissions,
                o =>
                    o.MapFrom(s =>
                        s.CompanyPermissions.Where(cp => cp.IsGranted).Select(cp => cp.Code)
                    )
            );

        CreateMap<UserCompany, UserCompanyProfileDTO>()
            .ForMember(d => d.Address, o => o.MapFrom(s => s.Address))
            .ForMember(d => d.CompanyFullName, o => o.MapFrom(s => s.Company.FullName))
            .ForMember(d => d.CompanyName, o => o.MapFrom(s => s.Company.CompanyName))
            .ForMember(d => d.CompanyBrand, o => o.MapFrom(s => s.Company.Brand))
            .ForMember(d => d.CompanyIsIndividual, o => o.MapFrom(s => !s.Company.IsCompany))
            .ForMember(d => d.CompanyDomain, o => o.MapFrom(s => s.Company.Domain))
            .ForMember(d => d.CompanyAddress, o => o.MapFrom(s => s.Company.Address))
            // CORREGIDO: Información del CustomPlan
            .ForMember(d => d.CustomPlanId, o => o.MapFrom(s => s.Company.CustomPlanId))
            .ForMember(d => d.CustomPlanPrice, o => o.MapFrom(s => s.Company.CustomPlan.Price))
            .ForMember(
                d => d.CustomPlanIsActive,
                o => o.MapFrom(s => s.Company.CustomPlan.IsActive)
            )
            .ForMember(
                d => d.RoleNames,
                o => o.MapFrom(s => s.UserCompanyRoles.Select(ucr => ucr.Role.Name))
            )
            // CORREGIDO: Módulos adicionales del CustomPlan
            .ForMember(
                d => d.AdditionalModules,
                o =>
                    o.MapFrom(s =>
                        s.Company.CustomPlan.CustomModules.Where(cm => cm.IsIncluded)
                            .Select(cm => cm.Module.Name)
                    )
            )
            .ForMember(
                d => d.EffectivePermissions,
                o =>
                    o.MapFrom(s =>
                        // Permisos de roles
                        s.UserCompanyRoles.SelectMany(ucr => ucr.Role.RolePermissions)
                            .Select(rp => rp.Permission.Code)
                            .Concat(
                                // Permisos personalizados granted
                                s.CompanyPermissions.Where(cp => cp.IsGranted)
                                    .Select(cp => cp.Code)
                            )
                            .Except(
                                // Menos permisos personalizados revoked
                                s.CompanyPermissions.Where(cp => !cp.IsGranted)
                                    .Select(cp => cp.Code)
                            )
                            .Distinct()
                    )
            );

        // Command mappings
        CreateMap<NewUserCompanyDTO, CreateUserCompanyCommand>()
            .ConstructUsing(src => new CreateUserCompanyCommand(src, string.Empty));

        CreateMap<UpdateUserCompanyDTO, UpdateUserCompanyCommand>()
            .ConstructUsing(src => new UpdateUserCompanyCommand(src));
    }
}
