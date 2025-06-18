using AutoMapper;
using Entities;
using signature.Application.DTOs;

namespace signature.Application.Profiles;

public class SignatureProfile : Profile
{
    public SignatureProfile()
    {
        CreateMap<CreateSignatureRequestDto, SignatureRequest>().ReverseMap();
        CreateMap<SignerInfoDto, Signer>().ReverseMap();
        CreateMap<DigitalCertificateDto, DigitalCertificate>();
    }
}
