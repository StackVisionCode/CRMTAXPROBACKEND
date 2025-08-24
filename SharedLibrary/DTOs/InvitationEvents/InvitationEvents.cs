namespace SharedLibrary.DTOs.InvitationEvents;

public record InvitationCancelledEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid InvitationId,
    Guid CompanyId,
    string Email,
    string Token,
    Guid CancelledByUserId,
    string? CancellationReason,
    string? CompanyName,
    string? CompanyDomain
) : IntegrationEvent(Id, OccurredOn);
