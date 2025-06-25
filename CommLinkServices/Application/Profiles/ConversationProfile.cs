using AutoMapper;
using CommLinkServices.Application.DTOs;
using CommLinkServices.Domain;

namespace CommLinkServices.Application.Profiles;

public class ConversationProfile : Profile
{
    public ConversationProfile()
    {
        CreateMap<Conversation, ConversationDto>()
            .ForMember(
                d => d.OtherUserId,
                opt =>
                    opt.MapFrom(
                        (src, dest, destMember, context) =>
                            (Guid)context.Items["Me"]! == src.FirstUserId
                                ? src.SecondUserId
                                : src.FirstUserId
                    )
            );
    }
}
