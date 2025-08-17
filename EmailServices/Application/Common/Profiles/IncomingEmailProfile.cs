using Application.Common.DTO;
using AutoMapper;
using Domain;

namespace Application.Common.Profiles;

public class IncomingEmailProfile : Profile
{
    public IncomingEmailProfile()
    {
        // Domain to DTO
        CreateMap<IncomingEmail, IncomingEmailDTO>();

        // DTO to Domain
        CreateMap<IncomingEmailDTO, IncomingEmail>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.ReceivedOn, opt => opt.Ignore()); // Se setea automáticamente
    }
}
