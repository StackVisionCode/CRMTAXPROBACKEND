namespace SharedLibrary.DTOs.AuthEvents;

public sealed record AccountConfirmationLinkEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string DisplayName,
    string ConfirmLink,
    DateTime ExpiresAt,
    bool IsCompany,
    string? CompanyName = null,
    string? AdminName = null,
    string? Domain = null
) : IntegrationEvent(Id, OccurredOn);
