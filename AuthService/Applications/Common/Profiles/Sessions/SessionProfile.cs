using AuthService.Domains.Sessions;
using AuthService.DTOs.SessionDTOs;
using AutoMapper;

namespace AuthService.Profiles.Sessions;

public class SessionProfile : Profile
{
    public SessionProfile()
    {
        CreateMap<Session, SessionDTO>()
            .ForMember(
                dest => dest.Location,
                opt => opt.MapFrom(src => $"{src.Latitude},{src.Logintude}")
            )
            .ReverseMap()
            .ForMember(dest => dest.Latitude, opt => opt.Ignore())
            .ForMember(dest => dest.Logintude, opt => opt.Ignore());
    }
}
