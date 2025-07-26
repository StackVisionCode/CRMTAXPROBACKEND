using Application.Common.DTO;
using AutoMapper;
using Domain;

namespace Application.Common.Profiles;

public class EmailAttachmentProfile : Profile
{
    public EmailAttachmentProfile()
    {
        CreateMap<EmailAttachment, EmailAttachmentDTO>();
        CreateMap<EmailAttachmentDTO, EmailAttachment>().ForMember(d => d.Id, o => o.Ignore());
    }
}
