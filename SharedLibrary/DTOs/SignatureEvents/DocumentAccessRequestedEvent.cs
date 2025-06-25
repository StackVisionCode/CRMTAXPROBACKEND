namespace SharedLibrary.DTOs.SignatureEvents;

public sealed record DocumentAccessRequestedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid DocumentId,
    Guid SignerId,
    string SignerEmail,
    string AccessToken, // Token temporal para acceso al documento
    DateTime ExpiresAt, // Cuándo expira el acceso
    string SessionId // ID de sesión único para tracking
) : IntegrationEvent(Id, OccurredOn);
