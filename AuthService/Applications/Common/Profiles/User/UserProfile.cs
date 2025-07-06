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
        CreateMap<UserProfileDTO, TaxUser>()
            .ReverseMap()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.TaxUserProfile.Name))
            .ForMember(d => d.LastName, o => o.MapFrom(s => s.TaxUserProfile.LastName))
            .ForMember(d => d.Address, o => o.MapFrom(s => s.TaxUserProfile.Address))
            .ForMember(d => d.PhotoUrl, o => o.MapFrom(s => s.TaxUserProfile.PhotoUrl))
            .ForMember(
                d => d.RoleNames,
                o => o.MapFrom(s => s.UserRoles.Select(ur => ur.Role.Name))
            )
            .ForMember(
                d => d.FullName,
                o => o.MapFrom(s => s.Company != null ? s.Company.FullName : null)
            )
            .ForMember(
                d => d.CompanyName,
                o => o.MapFrom(s => s.Company != null ? s.Company.CompanyName : null)
            )
            .ForMember(
                d => d.CompanyBrand,
                o => o.MapFrom(s => s.Company != null ? s.Company.Brand : null)
            )
            .ReverseMap();
        CreateMap<NewUserDTO, CreateTaxUserCommands>().ReverseMap();
        CreateMap<UpdateUserDTO, UpdateTaxUserCommands>().ReverseMap();
        CreateMap<CreateTaxUserCommands, TaxUser>().ReverseMap();
    }
}
