using AutoMapper;
using CustomerService.Commands.FilingStatusCommands;
using CustomerService.Domains.Customers;
using CustomerService.DTOs.FilingStatusDTOs;

namespace CustomerService.Profiles.FilingStatuses;

public class FilingStatusProfile : Profile
{
    public FilingStatusProfile()
    {
        CreateMap<CreateFilingStatusDTO, FilingStatus>().ReverseMap();
        CreateMap<ReadFilingStatusDto, FilingStatus>().ReverseMap();
        CreateMap<CreateFilingStatusCommands, FilingStatus>().ReverseMap();
    }
}
