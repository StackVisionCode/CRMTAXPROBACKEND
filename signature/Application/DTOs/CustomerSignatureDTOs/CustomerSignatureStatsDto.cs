using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.CustomerSignatureDTOs;

/// <summary>
/// DTO para estadísticas de cliente
/// </summary>
public class CustomerSignatureStatsDto
{
    [Key]
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public int ExpiredRequests { get; set; }
    public double CompletionRate { get; set; }
    public double AvgCompletionTimeHours { get; set; }
    public int TotalSigners { get; set; }
    public int ActiveSigners { get; set; }
    public int UrgentRequests { get; set; }
    public DateTime? LastActivity { get; set; }
    public List<MonthlyTrendDto> MonthlyTrend { get; set; } = new();
}
