using AuthService.Domains.Users;
using AuthService.DTOs.UserDTOs;
using AutoMapper;
using Commands.UserCommands;

namespace AuthService.Profiles.User;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // Basic mappings
        CreateMap<UpdateUserDTO, TaxUser>()
            .ForMember(d => d.AddressId, o => o.Ignore())
            .ForMember(d => d.Address, o => o.Ignore());

        CreateMap<TaxUser, UserGetDTO>()
            .ForMember(d => d.Address, o => o.MapFrom(s => s.Address))
            .ForMember(d => d.CompanyFullName, o => o.MapFrom(s => s.Company.FullName))
            .ForMember(d => d.CompanyName, o => o.MapFrom(s => s.Company.CompanyName))
            .ForMember(d => d.CompanyBrand, o => o.MapFrom(s => s.Company.Brand))
            .ForMember(d => d.CompanyIsIndividual, o => o.MapFrom(s => !s.Company.IsCompany))
            .ForMember(d => d.CompanyDomain, o => o.MapFrom(s => s.Company.Domain))
            .ForMember(d => d.CompanyAddress, o => o.MapFrom(s => s.Company.Address))
            .ForMember(
                d => d.RoleNames,
                o => o.MapFrom(s => s.UserRoles.Select(ur => ur.Role.Name))
            );

        CreateMap<TaxUser, UserProfileDTO>()
            .ForMember(d => d.Address, o => o.MapFrom(s => s.Address))
            .ForMember(d => d.CompanyFullName, o => o.MapFrom(s => s.Company.FullName))
            .ForMember(d => d.CompanyName, o => o.MapFrom(s => s.Company.CompanyName))
            .ForMember(d => d.CompanyBrand, o => o.MapFrom(s => s.Company.Brand))
            .ForMember(d => d.CompanyIsIndividual, o => o.MapFrom(s => !s.Company.IsCompany))
            .ForMember(d => d.CompanyDomain, o => o.MapFrom(s => s.Company.Domain))
            .ForMember(d => d.CompanyAddress, o => o.MapFrom(s => s.Company.Address))
            .ForMember(
                d => d.RoleNames,
                o => o.MapFrom(s => s.UserRoles.Select(ur => ur.Role.Name))
            );

        CreateMap<UpdateUserDTO, UpdateTaxUserCommands>()
            .ConstructUsing(src => new UpdateTaxUserCommands(src));
    }
}
