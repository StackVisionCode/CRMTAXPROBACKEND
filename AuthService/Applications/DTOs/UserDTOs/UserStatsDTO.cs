namespace AuthService.DTOs.UserDTOs;

/// <summary>
/// DTO para estadísticas de usuarios de una company
/// </summary>
public class UserStatsDTO
{
    public Guid CompanyId { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int OwnerCount { get; set; }
    public int RegularUserCount { get; set; }
    public int ConfirmedUsers { get; set; }
    public int PendingConfirmation { get; set; }
    public int PlanUserLimit { get; set; }
    public int AvailableSlots { get; set; }
    public bool IsWithinLimits { get; set; }
    public int UsagePercentage { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = new();
    public DateTime LastUserCreated { get; set; }
}
