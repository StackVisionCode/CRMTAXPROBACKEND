using AuthService.DTOs.SessionDTOs;
using AuthService.Domains.Sessions;
using AutoMapper;

namespace AuthService.Profiles.Sessions;

public class SessionProfile : Profile
{
    public SessionProfile()
    {
        CreateMap<SessionDTO, Session>().ReverseMap();
    }
}