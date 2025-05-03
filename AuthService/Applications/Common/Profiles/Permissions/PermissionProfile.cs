using AuthService.DTOs.PermissionDTOs;
using AuthService.Domains.Permissions;
using AutoMapper;

namespace AuthService.Profiles.Permissions;

public class PermissionProfile : Profile
{
  public PermissionProfile()
  {
    CreateMap<PermissionDTO, Permission>().ReverseMap();
  }
}