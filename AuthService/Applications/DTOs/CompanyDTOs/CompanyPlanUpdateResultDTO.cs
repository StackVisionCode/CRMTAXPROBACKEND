namespace AuthService.DTOs.CompanyDTOs;

/// <summary>
/// Resultado detallado del cambio de plan
/// </summary>
public class CompanyPlanUpdateResultDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string PreviousPlan { get; set; } = string.Empty;
    public string NewPlan { get; set; } = string.Empty;
    public decimal PreviousPrice { get; set; }
    public decimal NewPrice { get; set; }
    public int PreviousUserLimit { get; set; }
    public int NewUserLimit { get; set; }
    public int ActiveUsersCount { get; set; }
    public int DeactivatedUsersCount { get; set; }
    public ICollection<string> DeactivatedUserEmails { get; set; } = new List<string>();
    public ICollection<string> AddedModules { get; set; } = new List<string>();
    public ICollection<string> RemovedModules { get; set; } = new List<string>();
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
}
