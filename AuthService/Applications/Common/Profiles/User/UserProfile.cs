using AuthService.Applications.Common;
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
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.Company, o => o.Ignore())
            .ForMember(d => d.IsOwner, o => o.Ignore())
            .ForMember(d => d.Password, o => o.Ignore())
            .ForMember(d => d.AddressId, o => o.Ignore())
            .ForMember(d => d.Address, o => o.Ignore())
            .ForMember(d => d.Sessions, o => o.Ignore())
            .ForMember(d => d.UserRoles, o => o.Ignore())
            .ForMember(d => d.CompanyPermissions, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.DeleteAt, o => o.Ignore())
            .ForMember(d => d.Confirm, o => o.Ignore())
            .ForMember(d => d.ConfirmToken, o => o.Ignore())
            .ForMember(d => d.ResetPasswordToken, o => o.Ignore())
            .ForMember(d => d.ResetPasswordExpires, o => o.Ignore())
            .ForMember(d => d.Factor2, o => o.Ignore())
            .ForMember(d => d.Otp, o => o.Ignore())
            .ForMember(d => d.OtpVerified, o => o.Ignore())
            .ForMember(d => d.OtpExpires, o => o.Ignore())
            // Campos condicionales
            .ForMember(d => d.Email, o => o.Condition(src => !string.IsNullOrEmpty(src.Email)))
            .ForMember(d => d.Name, o => o.Condition(src => src.Name != null))
            .ForMember(d => d.LastName, o => o.Condition(src => src.LastName != null))
            .ForMember(d => d.PhoneNumber, o => o.Condition(src => src.PhoneNumber != null))
            .ForMember(d => d.PhotoUrl, o => o.Condition(src => src.PhotoUrl != null))
            .ForMember(d => d.IsActive, o => o.Condition(src => src.IsActive.HasValue));

        CreateMap<TaxUser, UserGetDTO>()
            .ForMember(d => d.Address, o => o.MapFrom(s => s.Address))
            .ForMember(d => d.IsOwner, o => o.MapFrom(s => s.IsOwner))
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
            // CompanyServiceLevel
            .ForMember(
                d => d.CompanyServiceLevel,
                o => o.MapFrom(s => s.Company != null ? s.Company.ServiceLevel : ServiceLevel.Basic)
            )
            .ForMember(
                d => d.RoleNames,
                o => o.MapFrom(s => s.UserRoles.Select(ur => ur.Role.Name))
            )
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
            .ForMember(d => d.IsOwner, o => o.MapFrom(s => s.IsOwner))
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
            // CompanyServiceLevel
            .ForMember(
                d => d.CompanyServiceLevel,
                o => o.MapFrom(s => s.Company != null ? s.Company.ServiceLevel : ServiceLevel.Basic)
            )
            .ForMember(
                d => d.RoleNames,
                o => o.MapFrom(s => s.UserRoles.Select(ur => ur.Role.Name))
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
