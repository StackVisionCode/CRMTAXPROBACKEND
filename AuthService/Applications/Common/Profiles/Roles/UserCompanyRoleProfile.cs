using AuthService.Domains.Roles;
using AuthService.DTOs.RoleDTOs;
using AutoMapper;

namespace AuthService.Profiles.Roles;

public class UserCompanyRoleProfile : Profile
{
    public UserCompanyRoleProfile()
    {
        CreateMap<UserCompanyRole, UserRoleDTO>()
            .ForMember(d => d.TaxUserId, o => o.MapFrom(s => s.UserCompanyId))
            .ForMember(d => d.RoleId, o => o.MapFrom(s => s.RoleId));

        CreateMap<UserRoleDTO, UserCompanyRole>()
            .ForMember(d => d.UserCompanyId, o => o.MapFrom(s => s.TaxUserId))
            .ForMember(d => d.RoleId, o => o.MapFrom(s => s.RoleId))
            .ForMember(d => d.UserCompany, o => o.Ignore())
            .ForMember(d => d.Role, o => o.Ignore())
            .ForMember(d => d.CompanyPermissions, o => o.Ignore());
    }
}
