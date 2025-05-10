using AutoMapper;
using DTOs.FirmsDto;
using Domains.Firms;


namespace Profiles.Firms;

public class FirmsProfile : Profile
{
    public FirmsProfile()
    {
        CreateMap<CreateFirmDto, Firm>().ReverseMap();
        CreateMap<Firm, CreateFirmDto>().ReverseMap();

        CreateMap<UpdateFirmDto, Firm>().ReverseMap();
        CreateMap<Firm, UpdateFirmDto>().ReverseMap();

        // Correct ReadFirmDto mapping
        CreateMap<Firm, ReadFirmDto>()
            .ForMember(dest => dest.FirmStatus, opt => opt.MapFrom(src => src.FirmStatus != null ? src.FirmStatus.Name : null))
            .ForMember(dest => dest.SignatureType, opt => opt.MapFrom(src => src.SignatureType != null ? src.SignatureType.Name : null));
        CreateMap<Firm, ReadFirmDto>().ReverseMap();


        CreateMap<DeleteFirmDto, Firm>().ReverseMap();
        CreateMap<Firm, DeleteFirmDto>().ReverseMap();


    }
}