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
    CreateMap<UserDTO, TaxUser>().ReverseMap();
       CreateMap<NewUserDTO, CreateTaxUserCommands>().ReverseMap();
        CreateMap<CreateTaxUserCommands, TaxUser>().ReverseMap();
  }
}