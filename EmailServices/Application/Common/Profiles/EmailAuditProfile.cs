using Application.Common.DTO;
using AutoMapper;
using Domain;

namespace Application.Common.Profiles;

public class EmailAuditProfile : Profile
{
    public EmailAuditProfile()
    {
        // Mapeo para operaciones de auditoría
        CreateMap<EmailConfig, EmailConfigDTO>()
            .ForMember(d => d.CreatedByTaxUserId, opt => opt.MapFrom(s => s.CreatedByTaxUserId))
            .ForMember(
                d => d.LastModifiedByTaxUserId,
                opt => opt.MapFrom(s => s.LastModifiedByTaxUserId)
            );

        CreateMap<Email, EmailDTO>()
            .ForMember(d => d.CreatedByTaxUserId, opt => opt.MapFrom(s => s.CreatedByTaxUserId))
            .ForMember(
                d => d.LastModifiedByTaxUserId,
                opt => opt.MapFrom(s => s.LastModifiedByTaxUserId)
            )
            .ForMember(d => d.SentByTaxUserId, opt => opt.MapFrom(s => s.SentByTaxUserId));

        CreateMap<EmailTemplate, EmailTemplateDTO>()
            .ForMember(d => d.CreatedByTaxUserId, opt => opt.MapFrom(s => s.CreatedByTaxUserId))
            .ForMember(
                d => d.LastModifiedByTaxUserId,
                opt => opt.MapFrom(s => s.LastModifiedByTaxUserId)
            );
    }
}
