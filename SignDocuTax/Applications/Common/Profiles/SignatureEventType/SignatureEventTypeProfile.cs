using AutoMapper;
using DTOs.SignatureEventTypeDto;

namespace Profiles.SignatureEventType;
public class SignatureEventTypeProfile : Profile
{
    public SignatureEventTypeProfile()
    {
        CreateMap<CreateSignatureEventTypeDto, Domains.Signatures.SignatureEventType>();
        CreateMap<UpdateSignatureEventTypeDto, Domains.Signatures.SignatureEventType>();
        CreateMap<Domains.Signatures.SignatureEventType, ReadSignatureEventTypeDto>();
    }
}

