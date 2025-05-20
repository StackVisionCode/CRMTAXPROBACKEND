namespace SharedLibrary.DTOs;

/// Evento publicado por AuthService al crear un TaxUser.
public sealed record UserCreatedEvent(
        Guid Id,
        DateTime OccurredOn,
        int UserId,
        string Email,
        string FullName)
    : IntegrationEvent(Id, OccurredOn);
