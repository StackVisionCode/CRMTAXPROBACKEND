using AutoMapper;
using CommLinkService.Domain.Entities;
using DTOs.RoomDTOs;

namespace CommLinkService.Profiles;

public class ParticipantProfile : Profile
{
    public ParticipantProfile()
    {
        CreateMap<RoomParticipant, RoomParticipantDTO>()
            .ForMember(dest => dest.DisplayName, opt => opt.Ignore())
            .ForMember(dest => dest.IsOnline, opt => opt.Ignore());
    }
}
