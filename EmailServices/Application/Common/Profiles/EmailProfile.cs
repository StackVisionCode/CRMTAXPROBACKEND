using Application.Common.DTO;
using AutoMapper;
using Domain;

namespace Application.Common.Profiles;

public class EmailProfile : Profile
{
    public EmailProfile()
    {
        CreateMap<Email, EmailDTO>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ReverseMap()
            .ForMember(d => d.Status, o => o.Ignore());
    }
}
