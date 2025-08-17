using AuthService.Domains.Users;
using AuthService.DTOs.UserDTOs;
using AutoMapper;
using Commands.UserCommands;

namespace AuthService.Profiles.User;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<UpdateUserDTO, TaxUser>()
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.Company, o => o.Ignore())
            .ForMember(d => d.IsOwner, o => o.Ignore()) // NUEVO: No se puede cambiar por DTO
            .ForMember(d => d.AddressId, o => o.Ignore())
            .ForMember(d => d.Address, o => o.Ignore())
            .ForMember(d => d.Sessions, o => o.Ignore())
            .ForMember(d => d.UserRoles, o => o.Ignore())
            .ForMember(d => d.CompanyPermissions, o => o.Ignore()); // NUEVO

        CreateMap<TaxUser, UserGetDTO>()
            .ForMember(d => d.Address, o => o.MapFrom(s => s.Address))
            .ForMember(d => d.IsOwner, o => o.MapFrom(s => s.IsOwner)) // NUEVO
            .ForMember(
                d => d.CompanyFullName,
                o => o.MapFrom(s => s.Company != null ? s.Company.FullName : null)
            )
            .ForMember(
                d => d.CompanyName,
                o => o.MapFrom(s => s.Company != null ? s.Company.CompanyName : null)
            )
            .ForMember(
                d => d.CompanyBrand,
                o => o.MapFrom(s => s.Company != null ? s.Company.Brand : null)
            )
            .ForMember(
                d => d.CompanyIsIndividual,
                o => o.MapFrom(s => s.Company != null ? !s.Company.IsCompany : false)
            )
            .ForMember(
                d => d.CompanyDomain,
                o => o.MapFrom(s => s.Company != null ? s.Company.Domain : null)
            )
            .ForMember(
                d => d.CompanyAddress,
                o => o.MapFrom(s => s.Company != null ? s.Company.Address : null)
            )
            .ForMember(
                d => d.RoleNames,
                o => o.MapFrom(s => s.UserRoles.Select(ur => ur.Role.Name))
            )
            // NUEVO: Permisos personalizados asignados por Administrator
            .ForMember(
                d => d.CustomPermissions,
                o =>
                    o.MapFrom(s =>
                        s.CompanyPermissions.Where(cp => cp.IsGranted)
                            .Select(cp => cp.Permission.Code)
                    )
            );

        CreateMap<TaxUser, UserProfileDTO>()
            .ForMember(d => d.Address, o => o.MapFrom(s => s.Address))
            .ForMember(d => d.IsOwner, o => o.MapFrom(s => s.IsOwner)) // NUEVO
            .ForMember(
                d => d.CompanyFullName,
                o => o.MapFrom(s => s.Company != null ? s.Company.FullName : null)
            )
            .ForMember(
                d => d.CompanyName,
                o => o.MapFrom(s => s.Company != null ? s.Company.CompanyName : null)
            )
            .ForMember(
                d => d.CompanyBrand,
                o => o.MapFrom(s => s.Company != null ? s.Company.Brand : null)
            )
            .ForMember(
                d => d.CompanyIsIndividual,
                o => o.MapFrom(s => s.Company != null ? !s.Company.IsCompany : false)
            )
            .ForMember(
                d => d.CompanyDomain,
                o => o.MapFrom(s => s.Company != null ? s.Company.Domain : null)
            )
            .ForMember(
                d => d.CompanyAddress,
                o => o.MapFrom(s => s.Company != null ? s.Company.Address : null)
            )
            .ForMember(
                d => d.RoleNames,
                o => o.MapFrom(s => s.UserRoles.Select(ur => ur.Role.Name))
            )
            // NUEVO: Información del CustomPlan de la Company
            .ForMember(
                d => d.CustomPlanId,
                o => o.MapFrom(s => s.Company != null ? s.Company.CustomPlanId : Guid.Empty)
            )
            .ForMember(
                d => d.CustomPlanPrice,
                o => o.MapFrom(s => s.Company != null ? s.Company.CustomPlan.Price : 0)
            )
            .ForMember(
                d => d.CustomPlanIsActive,
                o => o.MapFrom(s => s.Company != null ? s.Company.CustomPlan.IsActive : false)
            )
            .ForMember(
                d => d.AdditionalModules,
                o =>
                    o.MapFrom(s =>
                        s.Company != null
                            ? s
                                .Company.CustomPlan.CustomModules.Where(cm => cm.IsIncluded)
                                .Select(cm => cm.Module.Name)
                            : new List<string>()
                    )
            )
            .ForMember(
                d => d.EffectivePermissions,
                o =>
                    o.MapFrom(s =>
                        // Permisos de roles
                        s.UserRoles.SelectMany(ur => ur.Role.RolePermissions)
                            .Select(rp => rp.Permission.Code)
                            .Concat(
                                // Permisos personalizados granted
                                s.CompanyPermissions.Where(cp => cp.IsGranted)
                                    .Select(cp => cp.Permission.Code)
                            )
                            .Except(
                                // Menos permisos personalizados revoked
                                s.CompanyPermissions.Where(cp => !cp.IsGranted)
                                    .Select(cp => cp.Permission.Code)
                            )
                            .Distinct()
                    )
            );

        CreateMap<UpdateUserDTO, UpdateTaxUserCommands>()
            .ConstructUsing(src => new UpdateTaxUserCommands(src));
    }
}
