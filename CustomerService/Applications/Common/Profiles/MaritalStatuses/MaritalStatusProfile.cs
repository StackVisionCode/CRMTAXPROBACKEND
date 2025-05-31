using AutoMapper;
using CustomerService.Commands.MaritalStatusCommands;
using CustomerService.Domains.Customers;
using CustomerService.DTOs.MaritalStatusDTOs;

namespace CustomerService.Profiles.MaritalStatuses;

public class MaritalStatusProfile : Profile
{
    public MaritalStatusProfile()
    {
        CreateMap<CreateMaritalStatusDTO, MaritalStatus>().ReverseMap();
        CreateMap<ReadMaritalStatusDto, MaritalStatus>().ReverseMap();
        CreateMap<CreateMaritalStatusCommands, MaritalStatus>().ReverseMap();
    }
}
