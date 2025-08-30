namespace SharedLibrary.DTOs.InvitationEvents;

public record InvitationExpiredEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid InvitationId,
    Guid CompanyId,
    string Email,
    string Token,
    string? CompanyName,
    string? CompanyDomain
) : IntegrationEvent(Id, OccurredOn);
