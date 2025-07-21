using AuthService.Domains.Sessions;
using AuthService.DTOs.CompanyUserSessionDTOs;
using AutoMapper;

namespace AuthService.Profiles.CompanyUsers;

public class CompanyUserSessionProfile : Profile
{
    public CompanyUserSessionProfile()
    {
        CreateMap<CompanyUserSessionDTO, CompanyUserSession>().ReverseMap();
        CreateMap<ReadCompanyUserSessionDTO, CompanyUserSession>()
            .ReverseMap()
            .ForMember(d => d.SessionId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.LoginAt, o => o.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.ExpireAt, o => o.MapFrom(s => s.ExpireTokenRequest))
            .ForMember(d => d.Ip, o => o.MapFrom(s => s.IpAddress));
    }
}
