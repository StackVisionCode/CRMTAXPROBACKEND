using AutoMapper;
using CustomerService.Commands.PreferredContactCommads;
using CustomerService.Domains.Customers;
using CustomerService.DTOs.PreferredContactDTOs;

namespace CustomerService.Profiles.PreferredContacts;

public class PreferredContactProfile : Profile
{
    public PreferredContactProfile()
    {
        CreateMap<CreatePreferredContactDTO, PreferredContact>().ReverseMap();
        CreateMap<ReadPreferredContactDTO, PreferredContact>().ReverseMap();
        CreateMap<CreatePreferredContactCommands, PreferredContact>().ReverseMap();
    }
}
