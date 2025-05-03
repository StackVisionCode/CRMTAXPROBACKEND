using AuthService.DTOs.RoleDTOs;
using AuthService.Domains.Roles;
using AutoMapper;

namespace AuthService.Profiles.Roles;

public class RoleProfile : Profile
{
    public RoleProfile()
    {
        CreateMap<RoleDTO,Role>().ReverseMap();   
    }
}