using AutoMapper;
using CommLinkService.Domain.Entities;
using DTOs.ConnectionDTOs;

namespace CommLinkService.Profiles;

public class ConnectionProfile : Profile
{
    public ConnectionProfile()
    {
        CreateMap<Connection, ConnectionDTO>();
        CreateMap<ConnectionDTO, Connection>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeleteAt, opt => opt.Ignore());
    }
}
