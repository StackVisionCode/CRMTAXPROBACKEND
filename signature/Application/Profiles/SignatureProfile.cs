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

        CreateMap<SignerInfoDto, Signer>()
            .ForMember(dest => dest.InitialEntity, opt => opt.MapFrom(src => src.InitialEntity))
            .ForMember(dest => dest.FechaSigner, opt => opt.MapFrom(src => src.FechaSigner))
            .ForMember(d => d.CustomerId, c => c.MapFrom(s => s.CustomerId))
            .ReverseMap();

        CreateMap<DigitalCertificateDto, DigitalCertificate>();

        CreateMap<FechaSigner, FechaSignerDto>().ReverseMap();

        CreateMap<InitialEntityDto, IntialEntity>() // O usa el nombre corregido InitialEntity si lo renombras
            .ConstructUsing(src => new IntialEntity(
                src.InitalValue,
                src.WidthIntial,
                src.HeightIntial,
                src.PositionXIntial,
                src.PositionYIntial
            ));

        CreateMap<IntialEntity, InitialEntityDto>();
    }
}
