using Application.Common.DTO;
using AutoMapper;
using Domain;

namespace Application.Common.Profiles;

public class IncomingEmailProfile : Profile
{
    public IncomingEmailProfile()
    {
        CreateMap<IncomingEmail, IncomingEmailDTO>();
        CreateMap<IncomingEmailDTO, IncomingEmail>().ForMember(d => d.Id, o => o.Ignore());
    }
}
