namespace BankStaments.Domain.Tbl_IA_Entity;
public class SummaryInfo
{
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public List<CategorySummary> CategorySummaries { get; set; }
}
