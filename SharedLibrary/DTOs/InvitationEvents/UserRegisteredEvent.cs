namespace SharedLibrary.DTOs.InvitationEvents;

/// <summary>
/// Evento que se dispara cuando un User completa su registro por invitación
/// </summary>
public sealed record UserRegisteredEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid TaxUserId,
    string Email,
    string? Name,
    string? LastName,
    Guid CompanyId,
    string? CompanyName,
    string? CompanyFullName,
    string? CompanyDomain,
    bool IsCompany
) : IntegrationEvent(Id, OccurredOn);
