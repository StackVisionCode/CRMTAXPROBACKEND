namespace signature.Application.DTOs;

public class RejectSignatureDto
{
    public required string Token { get; set; }
    public string? Reason { get; set; }
}
