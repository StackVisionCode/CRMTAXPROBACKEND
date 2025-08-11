using SharedLibrary.DTOs;

namespace SharedLibrary.DTOs.InvitationEvents;

/// <summary>
/// Evento que se dispara cuando un UserCompany completa su registro por invitación
/// </summary>
public sealed record UserCompanyRegisteredEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserCompanyId,
    string Email,
    string? Name,
    string? LastName,
    Guid CompanyId,
    string? CompanyName,
    string? CompanyFullName,
    string? CompanyDomain,
    bool IsCompany
) : IntegrationEvent(Id, OccurredOn);
