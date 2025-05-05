using AuthService.Domains.Users;
using AuthService.DTOs.UserDTOs;

using AutoMapper;
using Commands.UserCommands;
using UserDTOS;

namespace AuthService.Profiles.User;

public class UserProfile : Profile
{
  public UserProfile()
  {
    CreateMap<NewUserDTO, TaxUser>().ReverseMap();
    CreateMap<UpdateUserDTO, TaxUser>().ReverseMap();
    CreateMap<UserDTO, TaxUser>().ReverseMap();
    CreateMap<UserGetDTO, TaxUser>().ReverseMap();
    CreateMap<UserProfileDTO, TaxUser>().ReverseMap();
    CreateMap<NewUserDTO, CreateTaxUserCommands>().ReverseMap();
    CreateMap<UpdateUserDTO, UpdateTaxUserCommands>().ReverseMap();
    CreateMap<CreateTaxUserCommands, TaxUser>().ReverseMap();
  }
}