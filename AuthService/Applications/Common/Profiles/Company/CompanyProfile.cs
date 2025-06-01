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
        CreateMap<UpdateCompanyDTO, Company>().ReverseMap();
        CreateMap<UpdateCompanyDTO, UpdateUserDTO>().ReverseMap();
        CreateMap<UpdateCompanyDTO, TaxUser>().ReverseMap();
        CreateMap<CreateTaxCompanyCommands, Company>().ReverseMap();
        CreateMap<UpdateTaxCompanyCommands, Company>().ReverseMap();
    }
}
