using AutoMapper;
using CustomerService.Commands.CustomerCommands;
using CustomerService.Domains.Customers;
using CustomerService.DTOs.CustomerDTOs;

namespace CustomerService.Profiles.Customers;

public class CustomerProfile : Profile
{
  public CustomerProfile()
  {
    CreateMap<CreateCustomerDTO, Customer>().ReverseMap();
    CreateMap<ReadCustomerDTO, Customer>().ReverseMap();
    CreateMap<CreateCustomerCommands, Customer>().ReverseMap();
  }
}