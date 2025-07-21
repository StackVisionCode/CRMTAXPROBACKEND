namespace Application.DTOs.CustomerSignatureDTOs;

/// <summary>
/// DTO para rendimiento de firmantes
/// </summary>
public class SignerPerformanceDto
{
    public Guid SignerId { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string SignerEmail { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public int TotalAssigned { get; set; }
    public int TotalSigned { get; set; }
    public int TotalRejected { get; set; }
    public int TotalPending { get; set; }
    public double CompletionRate => TotalAssigned > 0 ? (TotalSigned * 100.0) / TotalAssigned : 0;
    public double AvgTimeToSignHours { get; set; }
    public DateTime? LastActivity { get; set; }
    public string PerformanceRating { get; set; } = "average"; // excellent, good, average, poor
}
