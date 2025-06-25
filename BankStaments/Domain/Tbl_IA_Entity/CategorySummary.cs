namespace BankStaments.Domain.Tbl_IA_Entity;

public class CategorySummary
{
    public string Category { get; set; }
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
}