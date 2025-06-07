using Application.Common.DTO;
using AutoMapper;
using Domain;

namespace Application.Common.Profiles;

public class EmailConfigProfile : Profile
{
    public EmailConfigProfile()
    {
        CreateMap<EmailConfigDTO, EmailConfig>().ForMember(d => d.Id, o => o.Ignore());
        CreateMap<EmailConfig, EmailConfigDTO>();
    }
}
