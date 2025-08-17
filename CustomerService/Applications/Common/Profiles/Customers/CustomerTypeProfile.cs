using AutoMapper;
using CustomerService.Domains.Customers;
using CustomerService.DTOs.CustomerDTOs;

namespace CustomerService.Profiles.Customers;

public class CustomerTypeProfile : Profile
{
    public CustomerTypeProfile()
    {
        CreateMap<ReadCustomerTypeDTO, CustomerType>().ReverseMap();
    }
}
