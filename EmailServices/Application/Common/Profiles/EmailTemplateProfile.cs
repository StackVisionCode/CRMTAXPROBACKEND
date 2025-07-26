using Application.Common.DTO;
using AutoMapper;
using Domain;

namespace Application.Common.Profiles;

public class EmailTemplateProfile : Profile
{
    public EmailTemplateProfile()
    {
        CreateMap<EmailTemplate, EmailTemplateDTO>();
        CreateMap<EmailTemplateDTO, EmailTemplate>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedOn, o => o.Ignore())
            .ForMember(d => d.UpdatedOn, o => o.Ignore());
    }
}
