using AutoMapper;
using CommLinkService.Domain.Entities;
using DTOs.MessageDTOs;

namespace CommLinkService.Profiles;

public class MessageProfile : Profile
{
    public MessageProfile()
    {
        // Entity to DTO
        CreateMap<Message, MessageDTO>().ForMember(dest => dest.SenderName, opt => opt.Ignore());

        // DTO to Entity
        CreateMap<SendMessageDTO, Message>();
    }
}
