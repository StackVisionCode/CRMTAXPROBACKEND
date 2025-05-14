namespace DTOs.Signatures;

public record SignatureEventDetailDto(
    int Id,
    DateTime SignatureDate,
    string SignerType,
    string SignerName,
    string SignerEmail,
    string IpAddress,
    string DeviceInfo,
    string EventType,
    string? SignatureImageUrl);