using AuthService.Domains.Sessions;
using AuthService.DTOs.SessionDTOs;
using AutoMapper;

namespace AuthService.Profiles.Sessions;

public class SessionProfile : Profile
{
    public SessionProfile()
    {
        CreateMap<SessionDTO, Session>().ReverseMap();
    }
}
