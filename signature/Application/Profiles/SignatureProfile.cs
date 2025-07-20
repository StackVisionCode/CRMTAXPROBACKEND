using AutoMapper;
using Domain.Entities;
using signature.Application.DTOs;
using Signature.Application.DTOs;

namespace Signature.Application.Profiles;

public class SignatureProfile : Profile
{
    public SignatureProfile()
    {
        /* --- SignatureBox --- */
        // Solo mapeo de lectura (entidad -> DTO)
        CreateMap<SignatureBox, SignatureBoxDto>()
            .ForMember(d => d.Page, m => m.MapFrom(s => s.PageNumber))
            .ForMember(d => d.PosX, m => m.MapFrom(s => s.PositionX))
            .ForMember(d => d.PosY, m => m.MapFrom(s => s.PositionY))
            .ForMember(d => d.SignerId, m => m.MapFrom(s => s.SignerId));

        /* --- Signer --- */
        CreateMap<SignerInfoDto, Signer>()
            .ForMember(d => d.Boxes, m => m.Ignore()) // Se manejará manualmente
            .ReverseMap()
            .ForMember(d => d.Boxes, m => m.MapFrom(s => s.Boxes));

        /* --- Request --- */
        CreateMap<CreateSignatureRequestDto, SignatureRequest>().ReverseMap();

        /* --- Value-objects --- */
        CreateMap<DigitalCertificateDto, DigitalCertificate>().ReverseMap();
        CreateMap<InitialEntityDto, IntialEntity>().ReverseMap();
        CreateMap<FechaSignerDto, FechaSigner>().ReverseMap();
    }
}
