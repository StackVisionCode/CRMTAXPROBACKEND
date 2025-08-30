using Applications.DTOs.AddressDTOs;
using AuthService.Domains.Addresses;
using AutoMapper;

public class AddressProfile : Profile
{
    public AddressProfile()
    {
        CreateMap<AddressDTO, Address>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeleteAt, opt => opt.Ignore())
            .ForMember(dest => dest.Country, opt => opt.Ignore())
            .ForMember(dest => dest.State, opt => opt.Ignore());

        CreateMap<Address, AddressDTO>()
            .ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.Country.Name))
            .ForMember(dest => dest.StateName, opt => opt.MapFrom(src => src.State.Name));
    }
}
