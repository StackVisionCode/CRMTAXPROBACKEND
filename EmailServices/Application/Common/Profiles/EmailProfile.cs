using Application.Common.DTO;
using AutoMapper;
using Domain;

namespace Application.Common.Profiles;

public class EmailProfile : Profile
{
    public EmailProfile()
    {
        // Domain to DTO
        CreateMap<Email, EmailDTO>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

        // Create DTO to Domain
        CreateMap<CreateEmailDTO, Email>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(
                d => d.Status,
                opt => opt.MapFrom(src => EmailServices.Domain.EmailStatus.Pending)
            )
            .ForMember(d => d.CreatedOn, opt => opt.Ignore())
            .ForMember(d => d.UpdatedOn, opt => opt.Ignore())
            .ForMember(d => d.SentOn, opt => opt.Ignore())
            .ForMember(d => d.ErrorMessage, opt => opt.Ignore())
            .ForMember(d => d.LastModifiedByTaxUserId, opt => opt.Ignore());

        // Create DTO to Domain
        CreateMap<UpdateEmailDTO, Email>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(
                d => d.Status,
                opt => opt.MapFrom(src => EmailServices.Domain.EmailStatus.Pending)
            )
            .ForMember(d => d.CreatedOn, opt => opt.Ignore())
            .ForMember(d => d.UpdatedOn, opt => opt.Ignore())
            .ForMember(d => d.SentOn, opt => opt.Ignore())
            .ForMember(d => d.ErrorMessage, opt => opt.Ignore())
            .ForMember(d => d.LastModifiedByTaxUserId, opt => opt.Ignore());

        // DTO to Domain (para compatibilidad)
        CreateMap<EmailDTO, Email>()
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.Id, opt => opt.Ignore());
    }
}
