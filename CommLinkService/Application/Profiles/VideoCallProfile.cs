using AutoMapper;
using CommLinkService.Domain.Entities;
using DTOs.VideoCallDTOs;

namespace CommLinkService.Profiles;

public class VideoCallProfile : Profile
{
    public VideoCallProfile()
    {
        CreateMap<Room, VideoCallDTO>()
            .ForMember(dest => dest.CallId, opt => opt.Ignore())
            .ForMember(dest => dest.RoomId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.StartedAt, opt => opt.Ignore())
            .ForMember(dest => dest.SignalServer, opt => opt.Ignore())
            .ForMember(dest => dest.IceServers, opt => opt.Ignore())
            .ForMember(dest => dest.Participants, opt => opt.Ignore());

        CreateMap<RoomParticipant, VideoCallParticipantDTO>()
            .ForMember(dest => dest.DisplayName, opt => opt.Ignore());
    }
}
