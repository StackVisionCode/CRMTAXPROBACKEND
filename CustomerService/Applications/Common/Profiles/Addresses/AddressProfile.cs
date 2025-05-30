using AutoMapper;
using CustomerService.Commands.AddressCommands;
using CustomerService.Domains.Customers;
using CustomerService.DTOs.AddressDTOs;

namespace CustomerService.Profiles.Addresses;

public class AddressProfile : Profile
{
  public AddressProfile()
  {
    CreateMap<CreateAddressDTO, Address>().ReverseMap();
    CreateMap<ReadAddressDTO, Address>().ReverseMap();
    CreateMap<CreateAddressCommands, Address>().ReverseMap();
  }
}