using AuthService.Domains.Users;
using AuthService.DTOs.RoleDTOs;
using AutoMapper;

namespace AuthService.Profiles.Roles;

public class UserRoleProfile : Profile
{
    public UserRoleProfile()
    {
        CreateMap<UserRoleDTO, UserRole>().ReverseMap();
    }
}
