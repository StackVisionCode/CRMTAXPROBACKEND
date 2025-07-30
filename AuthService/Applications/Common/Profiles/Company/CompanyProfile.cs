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
        CreateMap<NewCompanyDTO, Company>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.AddressId, o => o.Ignore()) // se setea en handler tras crear Address
            .ForMember(d => d.Address, o => o.Ignore())
            .ForMember(d => d.TaxUsers, o => o.Ignore());

        CreateMap<UpdateCompanyDTO, Company>()
            .ForMember(d => d.AddressId, o => o.Ignore())
            .ForMember(d => d.Address, o => o.Ignore());

        CreateMap<Company, CompanyDTO>()
            .ForMember(
                dest => dest.CurrentUserCount,
                opt => opt.MapFrom(src => src.TaxUsers.Count())
            )
            .ForMember(d => d.Address, o => o.MapFrom(s => s.Address));

        // Command mappings
        CreateMap<NewCompanyDTO, CreateTaxCompanyCommands>()
            .ConstructUsing(src => new CreateTaxCompanyCommands(src, string.Empty));

        CreateMap<UpdateCompanyDTO, UpdateTaxCompanyCommands>()
            .ConstructUsing(src => new UpdateTaxCompanyCommands(src));
    }
}
