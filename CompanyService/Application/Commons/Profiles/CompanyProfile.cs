using AutoMapper;
using CompanyService.Application.DTOs;
using CompanyService.Domains;
using CompanyService.Infraestructure.Commands;

namespace Application.Commons.Profiles;


public class CompanyProfile : Profile
{
    public CompanyProfile()
    {
        CreateMap<CompanyDto, Company>().ReverseMap();
        CreateMap<CreateCompanyCommand, CompanyDto>().ReverseMap();

    }
}