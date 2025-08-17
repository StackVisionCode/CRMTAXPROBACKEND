using AuthService.Domains.Permissions;
using AuthService.DTOs.PermissionDTOs;
using AutoMapper;

namespace AuthService.Profiles.Permissions;

public class PermissionProfile : Profile
{
    public PermissionProfile()
    {
        CreateMap<PermissionDTO, Permission>()
            .ForMember(d => d.RolePermissions, o => o.Ignore())
            .ForMember(d => d.CompanyPermissions, o => o.Ignore());
        CreateMap<Permission, PermissionDTO>();
    }
}
