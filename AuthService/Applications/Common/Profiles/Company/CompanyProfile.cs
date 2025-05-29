using AuthService.Applications.DTOs.CompanyDTOs;
using AuthService.Domains.Companies;
using AuthService.Domains.Users;
using AuthService.DTOs.UserDTOs;
using AutoMapper;
using Commands.UserCommands;

namespace AuthService.Profiles.User;

public class CompanyProfile : Profile
{

  public CompanyProfile()
  {
    CreateMap<NewCompanyDTO, Company>().ReverseMap();
    CreateMap<NewCompanyDTO, NewUserDTO>().ReverseMap();
    CreateMap<CreateTaxCompanyCommands, Company>().ReverseMap();
  }
}