using AutoMapper;
using CustomerService.Commands.TaxInformationCommands;
using CustomerService.Domains.Customers;
using CustomerService.DTOs.TaxInformationDTOs;

namespace CustomerService.Profiles.TaxInformations;

public class TaxInformationProfile : Profile
{
  public TaxInformationProfile()
  {
    CreateMap<CreateTaxInformationDTOs, TaxInformation>().ReverseMap();
    CreateMap<ReadTaxInformationDTO, TaxInformation>().ReverseMap();
    CreateMap<CreateTaxInformationCommands, TaxInformation>().ReverseMap();
  }
}