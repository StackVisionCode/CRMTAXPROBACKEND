namespace AuthService.DTOs.ServiceDTOs;

public class ServiceWithStatsDTO : ServiceDTO
{
    public int CompaniesUsingCount { get; set; }
    public int TotalActiveUsers { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOwnersUsing { get; set; }
    public int TotalRegularUsersUsing { get; set; }
    public decimal AverageRevenuePerCompany { get; set; }
    public double AverageUsersPerCompany { get; set; }
    public decimal RevenuePerUser { get; set; }
}
