namespace SharedLibrary.DTOs;

/// Evento publicado por AuthService al crear un TaxUser.
public sealed record UserCreatedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string Name,
    string LastName
) : IntegrationEvent(Id, OccurredOn);
