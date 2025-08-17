namespace AuthService.DTOs.CustomPlanDTOs;

public class CustomPlanWithStatsDTO : CustomPlanDTO
{
    // Estadísticas de usuarios
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int OwnerCount { get; set; }
    public int RegularUsersCount { get; set; }

    // Información del servicio
    public string? BaseServiceName { get; set; }
    public string? BaseServiceTitle { get; set; }
    public List<string> BaseServiceFeatures { get; set; } = new();
    public int ServiceUserLimit { get; set; }
    public bool IsWithinLimits { get; set; }

    // Módulos separados
    public List<string> BaseModuleNames { get; set; } = new();
    public List<string> ExtraModuleNames { get; set; } = new();

    // Métricas calculadas
    public decimal RevenuePerUser { get; set; }
    public double ModuleUtilization { get; set; }

    // Existentes actualizados
    public int TotalModules { get; set; }
    public int ActiveModules { get; set; }
    public bool IsExpired { get; set; }
    public int DaysUntilExpiry { get; set; }
    public decimal MonthlyRevenue { get; set; }
}
