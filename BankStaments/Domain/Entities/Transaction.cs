namespace Domain.Entities;
  public class Transaction
    {
        public Guid Id { get; set; }
        public Guid BankStatementId { get; set; }
        public BankStatement BankStatement { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } // "Credit" or "Debit"
        public string Category { get; set; }
    }