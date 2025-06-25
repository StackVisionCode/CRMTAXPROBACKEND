namespace BankStaments.Domain.Tbl_IA_Entity;

public class BankTransaction
{
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } // "credit", "debit"
    public string Category { get; set; }
    public string Reference { get; set; }
}