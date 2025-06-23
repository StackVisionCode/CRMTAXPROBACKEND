using AutoMapper;
using CommLinkServices.Application.DTOs;
using CommLinkServices.Domain;

namespace CommLinkServices.Application.Profiles;

public class MessageProfile : Profile
{
    public MessageProfile()
    {
        CreateMap<Message, MessageDto>();
        CreateMap<SendMessageRequestDto, Message>()
            .ForMember(d => d.Content, o => o.MapFrom(s => s.Content))
            .ForMember(d => d.HasAttachment, o => o.MapFrom(s => s.HasAttachment))
            .ForMember(d => d.AttachmentUrl, o => o.MapFrom(s => s.AttachmentUrl));
    }
}
