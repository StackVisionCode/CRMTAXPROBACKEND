namespace Domain.Entities;
  public class BankStatement
    {
       
          public Guid Id { get; set; }
        public string AccountNumber { get; set; }
        public DateTime StatementDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

