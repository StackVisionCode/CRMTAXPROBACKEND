using Application.Common.DTO;
using AutoMapper;
using Domain;

namespace Application.Common.Profiles;

public class EmailTemplateProfile : Profile
{
    public EmailTemplateProfile()
    {
        // Domain to DTO
        CreateMap<EmailTemplate, EmailTemplateDTO>();

        // DTO to Domain
        CreateMap<EmailTemplateDTO, EmailTemplate>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedOn, opt => opt.Ignore())
            .ForMember(d => d.UpdatedOn, opt => opt.Ignore());

        // Create operations (sin Id)
        CreateMap<EmailTemplateDTO, EmailTemplate>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedOn, opt => opt.Ignore())
            .ForMember(d => d.UpdatedOn, opt => opt.Ignore())
            .ForMember(d => d.LastModifiedByTaxUserId, opt => opt.Ignore());
    }
}
