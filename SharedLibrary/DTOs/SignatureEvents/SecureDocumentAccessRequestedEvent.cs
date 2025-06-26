namespace SharedLibrary.DTOs.SignatureEvents;

public sealed record SecureDocumentAccessRequestedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid DocumentId,
    string EncryptedPayload, // Datos sensibles cifrados
    string PayloadHash, // Hash para verificar integridad
    DateTime ExpiresAt
) : IntegrationEvent(Id, OccurredOn);

// Payload que se cifra
public sealed record DocumentAccessPayload(
    Guid SignerId,
    string SignerEmail,
    string AccessToken,
    string SessionId,
    string RequestFingerprint // Huella única de la solicitud
);
