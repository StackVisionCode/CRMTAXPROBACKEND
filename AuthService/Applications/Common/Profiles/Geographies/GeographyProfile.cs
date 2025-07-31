using AuthService.Domains.Geography;
using AutoMapper;
using DTOs.GeographyDTOs;

namespace AuthService.Profiles.Geography;

public class GeographyProfile : Profile
{
    public GeographyProfile()
    {
        CreateMap<Country, CountryDTO>().ForMember(d => d.States, o => o.MapFrom(s => s.States));

        CreateMap<State, StateDTO>()
            .ForMember(d => d.CountryName, o => o.MapFrom(s => s.Country.Name));

        CreateMap<CountryDTO, Country>().ReverseMap();
        CreateMap<StateDTO, State>().ReverseMap();
    }
}
