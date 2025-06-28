using AuthService.Domains.Roles;
using AuthService.DTOs.RoleDTOs;
using AutoMapper;

namespace AuthService.Profiles.Roles;

public class RoleProfile : Profile
{
    public RoleProfile()
    {
        CreateMap<Role, RoleDTO>()
            .ForMember(
                d => d.PermissionCodes,
                o => o.MapFrom(s => s.RolePermissions.Select(rp => rp.Permission.Code))
            );
        CreateMap<RoleDTO, Role>().ReverseMap();
    }
}
