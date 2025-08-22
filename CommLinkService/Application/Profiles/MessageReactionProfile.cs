using AutoMapper;
using CommLinkService.Domain.Entities;
using DTOs.MessageDTOs;

namespace CommLinkService.Profiles;

public class MessageReactionProfile : Profile
{
    public MessageReactionProfile()
    {
        CreateMap<MessageReaction, MessageReactionDTO>()
            .ForMember(dest => dest.ReactorName, opt => opt.Ignore());
    }
}
