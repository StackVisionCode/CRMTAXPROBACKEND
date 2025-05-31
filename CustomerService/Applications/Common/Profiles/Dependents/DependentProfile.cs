using AutoMapper;
using CustomerService.Commands.DependentCommands;
using CustomerService.Domains.Customers;
using CustomerService.DTOs.DependentDTOs;

namespace CustomerService.Profiles.Dependents;

public class DependentProfile : Profile
{
  public DependentProfile()
  {
    CreateMap<CreateDependentDTO, Dependent>().ReverseMap();
    CreateMap<ReadDependentDTO, Dependent>().ReverseMap();
    CreateMap<CreateDependentCommands, Dependent>().ReverseMap();
  }
}