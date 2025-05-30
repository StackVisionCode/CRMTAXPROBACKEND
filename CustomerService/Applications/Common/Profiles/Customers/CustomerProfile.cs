using AutoMapper;
using CustomerService.Domains.Customers;
using CustomerService.DTOs.CustomerDTOs;

namespace CustomerService.Profiles.Customers;

public class CustomerProfile : Profile
{
  public CustomerProfile()
  {
    CreateMap<CreateCustomerDTO, Customer>().ReverseMap();
  }
}