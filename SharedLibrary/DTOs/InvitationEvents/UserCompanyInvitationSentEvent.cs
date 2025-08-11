using SharedLibrary.DTOs;

namespace SharedLibrary.DTOs.InvitationEvents;

/// <summary>
/// Evento que se dispara cuando se envía una invitación para UserCompany
/// </summary>
public sealed record UserCompanyInvitationSentEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid CompanyId,
    string Email,
    string InvitationLink,
    DateTime ExpiresAt,
    string? CompanyName,
    string? CompanyFullName,
    string? CompanyDomain,
    bool IsCompany,
    string? PersonalMessage
) : IntegrationEvent(Id, OccurredOn);
