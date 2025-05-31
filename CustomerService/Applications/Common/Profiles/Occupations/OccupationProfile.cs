using AutoMapper;
using CustomerService.Coommands.OccupationCommands;
using CustomerService.Domains.Customers;
using CustomerService.DTOs.OccupationDTOs;

namespace CustomerService.Profiles.OccupationProfiles;

public class OccupationProfile : Profile
{
    public OccupationProfile()
    {
        CreateMap<CreateOccupationDTO, Occupation>().ReverseMap();
        CreateMap<ReadOccupationDTO, Occupation>().ReverseMap();
        CreateMap<CreateOccupationCommands, Occupation>().ReverseMap();
    }
}
