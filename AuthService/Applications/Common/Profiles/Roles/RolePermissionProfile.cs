using AuthService.Domains.Roles;
using AuthService.DTOs.RoleDTOs;

using AutoMapper;

namespace AuthService.Profiles.Roles;

public class RolesPermissionsProfile : Profile
{
  public RolesPermissionsProfile()
  {
    CreateMap<RolePermissionDTO, RolePermissions>().ReverseMap();
  }
}