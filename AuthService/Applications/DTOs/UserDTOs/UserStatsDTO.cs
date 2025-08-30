using AuthService.Applications.Common;

namespace AuthService.DTOs.UserDTOs;

/// <summary>
/// DTO para estadísticas de usuarios de una company
/// </summary>
public class UserStatsDTO
{
    public Guid CompanyId { get; set; }
    public ServiceLevel CompanyServiceLevel { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int OwnerCount { get; set; }
    public int RegularUserCount { get; set; }
    public int ConfirmedUsers { get; set; }
    public int PendingConfirmation { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = new();
    public DateTime LastUserCreated { get; set; }
}
