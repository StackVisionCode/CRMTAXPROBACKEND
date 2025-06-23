namespace AuthService.DTOs.SessionDTOs;

public class ReadCustomerSessionDTO
{
    public Guid SessionId { get; set; }
    public DateTime LoginAt { get; set; }
    public DateTime ExpireAt { get; set; }
    public string? Ip { get; set; }
    public string? Device { get; set; }
    public bool IsRevoke { get; set; }
}
