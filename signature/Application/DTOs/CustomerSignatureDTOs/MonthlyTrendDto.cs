namespace Application.DTOs.CustomerSignatureDTOs;

/// <summary>
/// DTO para tendencia mensual
/// </summary>
public class MonthlyTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Completed { get; set; }
    public int Pending { get; set; }
    public int Rejected { get; set; }
    public int Total => Completed + Pending + Rejected;
}
