namespace SharedLibrary.DTOs.CommEvents.IdentityEvents;

public sealed record UserPresenceChangedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string UserType, // "TaxUser" | "Customer"
    bool IsOnline
) : IntegrationEvent(Id, OccurredOn);
