namespace AuthService.DTOs.SessionDTOs;

public class ReadCustomerSessionDTO
{
    public Guid SessionId { get; set; }
    public DateTime LoginAt { get; set; }
    public DateTime ExpireAt { get; set; }
    public string? Ip { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? Location { get; set; }
    public string? Device { get; set; }
    public bool IsRevoke { get; set; }
}
