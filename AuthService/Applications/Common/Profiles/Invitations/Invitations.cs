using AuthService.Commands.InvitationCommands;
using AuthService.Domains.Invitations;
using AuthService.DTOs.InvitationDTOs;
using AutoMapper;

namespace AuthService.Profiles.Invitations;

public class InvitationProfile : Profile
{
    public InvitationProfile()
    {
        // Entity -> DTO
        CreateMap<Invitation, InvitationDTO>()
            // Información del usuario que invitó
            .ForMember(
                d => d.InvitedByUserName,
                o => o.MapFrom(s => s.InvitedByUser.Name ?? string.Empty)
            )
            .ForMember(
                d => d.InvitedByUserLastName,
                o => o.MapFrom(s => s.InvitedByUser.LastName ?? string.Empty)
            )
            .ForMember(d => d.InvitedByUserEmail, o => o.MapFrom(s => s.InvitedByUser.Email))
            .ForMember(d => d.InvitedByUserIsOwner, o => o.MapFrom(s => s.InvitedByUser.IsOwner))
            // Información del usuario que canceló (opcional)
            .ForMember(
                d => d.CancelledByUserName,
                o => o.MapFrom(s => s.CancelledByUser != null ? s.CancelledByUser.Name : null)
            )
            .ForMember(
                d => d.CancelledByUserLastName,
                o => o.MapFrom(s => s.CancelledByUser != null ? s.CancelledByUser.LastName : null)
            )
            .ForMember(
                d => d.CancelledByUserEmail,
                o => o.MapFrom(s => s.CancelledByUser != null ? s.CancelledByUser.Email : null)
            )
            // Información del usuario registrado (opcional)
            .ForMember(
                d => d.RegisteredUserName,
                o => o.MapFrom(s => s.RegisteredUser != null ? s.RegisteredUser.Name : null)
            )
            .ForMember(
                d => d.RegisteredUserLastName,
                o => o.MapFrom(s => s.RegisteredUser != null ? s.RegisteredUser.LastName : null)
            )
            .ForMember(
                d => d.RegisteredUserEmail,
                o => o.MapFrom(s => s.RegisteredUser != null ? s.RegisteredUser.Email : null)
            )
            // Información de la company
            .ForMember(d => d.CompanyName, o => o.MapFrom(s => s.Company.CompanyName))
            .ForMember(d => d.CompanyFullName, o => o.MapFrom(s => s.Company.FullName))
            .ForMember(d => d.CompanyDomain, o => o.MapFrom(s => s.Company.Domain))
            .ForMember(d => d.CompanyIsCompany, o => o.MapFrom(s => s.Company.IsCompany))
            // RoleNames se mapea en el handler con consulta separada por performance
            .ForMember(d => d.RoleNames, o => o.Ignore());

        // DTO -> Entity (para creación)
        CreateMap<NewInvitationDTO, Invitation>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.InvitedByUserId, o => o.Ignore()) // Se asigna en el handler
            .ForMember(d => d.Token, o => o.Ignore()) // Se genera en el handler
            .ForMember(d => d.ExpiresAt, o => o.Ignore()) // Se calcula en el handler
            .ForMember(d => d.Status, o => o.MapFrom(src => InvitationStatus.Pending))
            .ForMember(d => d.RoleIds, o => o.MapFrom(src => src.RoleIds ?? new List<Guid>()))
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.DeleteAt, o => o.Ignore())
            // Navegación
            .ForMember(d => d.Company, o => o.Ignore())
            .ForMember(d => d.InvitedByUser, o => o.Ignore())
            .ForMember(d => d.CancelledByUser, o => o.Ignore())
            .ForMember(d => d.RegisteredUser, o => o.Ignore())
            // Campos de auditoría (se setean cuando corresponde)
            .ForMember(d => d.AcceptedAt, o => o.Ignore())
            .ForMember(d => d.CancelledAt, o => o.Ignore())
            .ForMember(d => d.CancelledByUserId, o => o.Ignore())
            .ForMember(d => d.CancellationReason, o => o.Ignore())
            .ForMember(d => d.RegisteredUserId, o => o.Ignore())
            .ForMember(d => d.InvitationLink, o => o.Ignore())
            .ForMember(d => d.IpAddress, o => o.Ignore())
            .ForMember(d => d.UserAgent, o => o.Ignore());

        // Command mappings
        CreateMap<NewInvitationDTO, SendUserInvitationCommand>()
            .ConstructUsing(src => new SendUserInvitationCommand(
                src,
                Guid.Empty,
                string.Empty,
                null,
                null
            ));

        CreateMap<CancelInvitationDTO, CancelInvitationCommand>()
            .ConstructUsing(src => new CancelInvitationCommand(src, Guid.Empty));

        CreateMap<CancelBulkInvitationsDTO, CancelBulkInvitationsCommand>()
            .ConstructUsing(src => new CancelBulkInvitationsCommand(src, Guid.Empty));
    }
}
