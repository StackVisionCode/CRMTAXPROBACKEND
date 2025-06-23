namespace AuthService.DTOs.SessionDTOs;

internal sealed class RemoteAuthInfoDTO
{
    public Guid CustomerId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsLogin { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
