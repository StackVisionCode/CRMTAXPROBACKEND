namespace SharedLibrary.DTOs;

/// Información del usuario que irá dentro del JWT
public sealed record UserInfo(Guid UserId, string Email, string? Name, string? LastName, string? Address, string? PhotoUrl, string? CompanyName, string? CompanyBrand);

/// Información de la sesión (podrían añadirse IP, device, etc.)
public sealed record SessionInfo(Guid Id);

/// Petición unificada para generar un token
public sealed record TokenGenerationRequest(UserInfo User, SessionInfo Session, TimeSpan LifeTime);

/// Resultado del token
public sealed record TokenResult( string AccessToken, DateTime ExpireAt);