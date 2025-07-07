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
        CreateMap<SignatureBoxDto, SignatureBox>()
            .ConstructUsing(
                (src, ctx) =>
                    new SignatureBox(
                        src.Page, // pageNumber
                        src.PosX, // posX
                        src.PosY, // posY
                        src.Width, // width
                        src.Height, // height
                        ctx.Mapper.Map<IntialEntity?>(src.InitialEntity),
                        ctx.Mapper.Map<FechaSigner?>(src.FechaSigner)
                    )
            )
            .ReverseMap()
            .ForMember(d => d.Page, m => m.MapFrom(s => s.PageNumber))
            .ForMember(d => d.PosX, m => m.MapFrom(s => s.PositionX))
            .ForMember(d => d.PosY, m => m.MapFrom(s => s.PositionY));

        /* --- Signer --- */
        CreateMap<SignerInfoDto, Signer>()
            .ForMember(d => d.Boxes, m => m.MapFrom(s => s.Boxes))
            .ReverseMap();

        /* --- Request --- */
        CreateMap<CreateSignatureRequestDto, SignatureRequest>().ReverseMap();

        /* --- Value-objects --- */
        CreateMap<DigitalCertificateDto, DigitalCertificate>().ReverseMap();
        CreateMap<InitialEntityDto, IntialEntity>().ReverseMap();
        CreateMap<FechaSignerDto, FechaSigner>().ReverseMap();
    }
}
