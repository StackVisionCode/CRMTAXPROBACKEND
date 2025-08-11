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
            .ForMember(d => d.UserCompany, o => o.Ignore())
            .ForMember(d => d.UserCompanyRole, o => o.Ignore());

        CreateMap<UpdateCompanyPermissionDTO, CompanyPermission>()
            .ForMember(d => d.UserCompanyId, o => o.Ignore())
            .ForMember(d => d.UserCompanyRoleId, o => o.Ignore())
            .ForMember(d => d.UserCompany, o => o.Ignore())
            .ForMember(d => d.UserCompanyRole, o => o.Ignore());

        CreateMap<CompanyPermission, CompanyPermissionDTO>()
            .ForMember(d => d.UserCompanyEmail, o => o.MapFrom(s => s.UserCompany.Email))
            .ForMember(d => d.RoleName, o => o.MapFrom(s => s.UserCompanyRole.Role.Name));

        // Command mappings
        CreateMap<AssignCompanyPermissionDTO, AssignCompanyPermissionCommand>()
            .ConstructUsing(src => new AssignCompanyPermissionCommand(src));

        CreateMap<UpdateCompanyPermissionDTO, UpdateCompanyPermissionCommand>()
            .ConstructUsing(src => new UpdateCompanyPermissionCommand(src));
    }
}
