namespace BankStaments.Domain.Tbl_IA_Entity;
public class ProcessedDocumentData
{
    public AccountInfo Account { get; set; }
    public StatementPeriod Period { get; set; }
    public List<BankTransaction> Transactions { get; set; }
    public SummaryInfo Summary { get; set; }
}