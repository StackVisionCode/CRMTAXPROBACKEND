namespace SharedLibrary.DTOs;

public sealed record TokenGenerationRequest(int UserId, string Email, string FullName, string SessionId, TimeSpan LifeTime);

public sealed record TokenResult( string AccessToken, DateTime ExpireAt);