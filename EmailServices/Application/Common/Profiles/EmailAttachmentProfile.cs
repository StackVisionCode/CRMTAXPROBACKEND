using Application.Common.DTO;
using AutoMapper;
using Domain;

namespace Application.Common.Profiles;

public class EmailAttachmentProfile : Profile
{
    public EmailAttachmentProfile()
    {
        // Domain to DTO
        CreateMap<EmailAttachment, EmailAttachmentDTO>();

        // DTO to Domain
        CreateMap<EmailAttachmentDTO, EmailAttachment>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedOn, opt => opt.Ignore());
    }
}
