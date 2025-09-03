using AutoMapper;
using LandingService.Applications.DTO;
using LandingService.Domain;
using LandingService.Infrastructure.Commands;

namespace LandingService.Applications.Profiles;

public class Perfiles : Profile
{
    public Perfiles()
    {
        CreateMap<RegisterDTO, User>().ReverseMap();
        CreateMap<CreateRegisterCommands, User>().ReverseMap();
        CreateMap<SessionDTO, Session>().ReverseMap();


    }
}