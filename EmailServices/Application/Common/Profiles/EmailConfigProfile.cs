using Application.Common.DTO;
using AutoMapper;
using Domain;

namespace Application.Common.Profiles;

public class EmailConfigProfile : Profile
{
    public EmailConfigProfile()
    {
        // Domain to DTO
        CreateMap<EmailConfig, EmailConfigDTO>();

        // Create DTO to Domain
        CreateMap<CreateEmailConfigDTO, EmailConfig>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedOn, opt => opt.Ignore())
            .ForMember(d => d.UpdatedOn, opt => opt.Ignore())
            .ForMember(d => d.LastModifiedByTaxUserId, opt => opt.Ignore());

        // Update DTO to Domain (para actualizaciones)
        CreateMap<UpdateEmailConfigDTO, EmailConfig>()
            .ForMember(d => d.CompanyId, opt => opt.Ignore()) // No se puede cambiar
            .ForMember(d => d.CreatedByTaxUserId, opt => opt.Ignore()) // No se puede cambiar
            .ForMember(d => d.CreatedOn, opt => opt.Ignore())
            .ForMember(d => d.UpdatedOn, opt => opt.MapFrom(src => DateTime.UtcNow));

        // DTO to Domain (para compatibilidad)
        CreateMap<EmailConfigDTO, EmailConfig>().ForMember(d => d.Id, opt => opt.Ignore());
    }
}
