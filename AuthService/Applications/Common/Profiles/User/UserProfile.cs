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
    CreateMap<UserProfileDTO, TaxUser>().ReverseMap()
            .ForMember(d => d.Name,      o => o.MapFrom(s => s.TaxUserProfile.Name))
            .ForMember(d => d.LastName,  o => o.MapFrom(s => s.TaxUserProfile.LastName))
            .ForMember(d => d.Address,   o => o.MapFrom(s => s.TaxUserProfile.Address))
            .ForMember(d => d.PhotoUrl,  o => o.MapFrom(s => s.TaxUserProfile.PhotoUrl))
            .ForMember(d => d.RoleName,  o => o.MapFrom(s => s.Role.Name))         
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => 
                src.Company != null ? src.Company.FullName : null))
            .ForMember(dest => dest.CompanyBrand, opt => opt.MapFrom(src => 
                src.Company != null ? src.Company.Brand : null));
    CreateMap<NewUserDTO, CreateTaxUserCommands>().ReverseMap();
    CreateMap<UpdateUserDTO, UpdateTaxUserCommands>().ReverseMap();
    CreateMap<CreateTaxUserCommands, TaxUser>().ReverseMap();
  }
}