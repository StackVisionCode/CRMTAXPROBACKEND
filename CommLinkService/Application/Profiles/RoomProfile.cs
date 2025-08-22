using AutoMapper;
using CommLinkService.Domain.Entities;
using DTOs.RoomDTOs;

namespace CommLinkService.Profiles;

public class RoomProfile : Profile
{
    public RoomProfile()
    {
        // Entity to DTO
        CreateMap<Room, RoomDTO>()
            .ForMember(
                dest => dest.ParticipantCount,
                opt => opt.MapFrom(src => src.Participants.Count)
            )
            .ForMember(dest => dest.UnreadCount, opt => opt.Ignore()) // Se calcula en el handler
            .ForMember(
                dest => dest.LastMessage,
                opt =>
                    opt.MapFrom(src =>
                        src.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()
                    )
            );

        // DTO to Entity
        CreateMap<CreateRoomDTO, Room>();

        CreateMap<Room, RoomInfoDto>()
            .ForMember(dest => dest.RoomId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedByTaxUserId));
    }
}
