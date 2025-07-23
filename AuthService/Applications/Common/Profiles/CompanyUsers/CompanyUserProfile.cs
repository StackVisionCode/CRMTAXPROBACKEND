using AuthService.Domains.CompanyUsers;
using AuthService.DTOs.CompanyUserDTOs;
using AutoMapper;
using Commands.CompanyUserCommands;

namespace AuthService.Profiles.CompanyUsers;

public class CompanyUserProfile : Profile
{
    public CompanyUserProfile()
    {
        CreateMap<NewCompanyUserDTO, CompanyUser>().ReverseMap();
        CreateMap<UpdateCompanyUserDTO, CompanyUser>().ReverseMap();
        CreateMap<CompanyUserGetDTO, CompanyUser>().ReverseMap();

        CreateMap<CompanyUserProfileDTO, CompanyUser>()
            .ReverseMap()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.CompanyUserProfile != null ? s.CompanyUserProfile.Name : null))
            .ForMember(d => d.LastName, o => o.MapFrom(s => s.CompanyUserProfile != null ? s.CompanyUserProfile.LastName : null))
            .ForMember(d => d.Address, o => o.MapFrom(s => s.CompanyUserProfile != null ? s.CompanyUserProfile.Address : null))
            .ForMember(d => d.PhotoUrl, o => o.MapFrom(s => s.CompanyUserProfile != null ? s.CompanyUserProfile.PhotoUrl : null))
            .ForMember(d => d.Position, o => o.MapFrom(s => s.CompanyUserProfile != null ? s.CompanyUserProfile.Position : null))
            .ForMember(
                d => d.RoleNames,
                o => o.MapFrom(s => s.CompanyUserRoles.Select(cur => cur.Role.Name))
            )
            .ForMember(
                d => d.CompanyName,
                o => o.MapFrom(s => s.Company != null ? s.Company.CompanyName : null)
            )
            .ForMember(
                d => d.CompanyFullName,
                o => o.MapFrom(s => s.Company != null ? s.Company.FullName : null)
            )
            .ForMember(
                d => d.CompanyBrand,
                o => o.MapFrom(s => s.Company != null ? s.Company.Brand : null)
            );

        CreateMap<NewCompanyUserDTO, CreateCompanyUserCommand>().ReverseMap();
        CreateMap<UpdateCompanyUserDTO, UpdateCompanyUserCommand>().ReverseMap();
        CreateMap<CreateCompanyUserCommand, CompanyUser>().ReverseMap();
    }
}
