using AutoMapper;
using CustomerService.Commands.ContactInfoCommands;
using CustomerService.Domains.Customers;
using CustomerService.DTOs.ContactInfoDTOs;

namespace CustomerService.Profiles.ContactInfos;

public class ContactInfoProfile : Profile
{
    public ContactInfoProfile()
    {
        CreateMap<CreateContactInfoDTOs, ContactInfo>().ReverseMap();
        CreateMap<UpdateContactInfoDTOs, ContactInfo>().ReverseMap();
        CreateMap<ReadContactInfoDTO, ContactInfo>().ReverseMap();
        CreateMap<CreateContactInfoCommands, ContactInfo>().ReverseMap();
        CreateMap<UpdateContactInfoCommands, ContactInfo>().ReverseMap();
    }
}
