namespace SharedLibrary.DTOs.CommEvents.IdentityEvents;

public sealed record UserCreatedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string UserType, // "TaxUser" | "Customer"
    string DisplayName,
    string Email
) : IntegrationEvent(Id, OccurredOn);
