using AuthService.Domains.Sessions;
using AuthService.DTOs.SessionDTOs;
using AutoMapper;

namespace AuthService.Profiles.UserCompanies;

public class UserCompanySessionProfile : Profile
{
    public UserCompanySessionProfile()
    {
        CreateMap<UserCompanySession, UserCompanySessionDTO>()
            .ForMember(
                dest => dest.Location,
                opt => opt.MapFrom(src => $"{src.Latitude},{src.Longitude}")
            )
            .ForMember(
                dest => dest.UserCompanyEmail,
                opt => opt.MapFrom(src => src.UserCompany.Email)
            )
            .ReverseMap()
            .ForMember(dest => dest.Latitude, opt => opt.Ignore())
            .ForMember(dest => dest.Longitude, opt => opt.Ignore())
            .ForMember(dest => dest.UserCompany, opt => opt.Ignore());
    }
}
