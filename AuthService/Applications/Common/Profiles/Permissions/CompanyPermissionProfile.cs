using AuthService.Domains.Permissions;
using AuthService.DTOs.CompanyPermissionDTOs;
using AutoMapper;
using Commands.CompanyPermissionCommands;

namespace AuthService.Profiles.Permissions;

public class CompanyPermissionProfile : Profile
{
    public CompanyPermissionProfile()
    {
        CreateMap<AssignCompanyPermissionDTO, CompanyPermission>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TaxUser, o => o.Ignore())
            .ForMember(d => d.Permission, o => o.Ignore());

        CreateMap<UpdateCompanyPermissionDTO, CompanyPermission>()
            .ForMember(d => d.TaxUserId, o => o.Ignore())
            .ForMember(d => d.PermissionId, o => o.Ignore())
            .ForMember(d => d.TaxUser, o => o.Ignore())
            .ForMember(d => d.Permission, o => o.Ignore());

        CreateMap<CompanyPermission, CompanyPermissionDTO>()
            .ForMember(d => d.UserEmail, o => o.MapFrom(s => s.TaxUser.Email))
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.TaxUser.Name))
            .ForMember(d => d.UserLastName, o => o.MapFrom(s => s.TaxUser.LastName))
            .ForMember(d => d.PermissionCode, o => o.MapFrom(s => s.Permission.Code))
            .ForMember(d => d.PermissionName, o => o.MapFrom(s => s.Permission.Name));

        CreateMap<AssignCompanyPermissionDTO, AssignCompanyPermissionCommand>()
            .ConstructUsing(src => new AssignCompanyPermissionCommand(src));

        CreateMap<UpdateCompanyPermissionDTO, UpdateCompanyPermissionCommand>()
            .ConstructUsing(src => new UpdateCompanyPermissionCommand(src));
    }
}
