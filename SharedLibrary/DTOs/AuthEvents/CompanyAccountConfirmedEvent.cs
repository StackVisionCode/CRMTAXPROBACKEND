namespace SharedLibrary.DTOs;

/// <summary>
/// Evento que se dispara cuando una cuenta de CompanyUser es confirmada y activada
/// </summary>
public sealed record CompanyAccountConfirmedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string DisplayName,
    string CompanyName,
    string FirstName,
    string LastName,
    string Position
) : IntegrationEvent(Id, OccurredOn);
