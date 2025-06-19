namespace Application.DTOS;

 public class TransactionDto
    {
        public Guid? Id { get; set; }
        public string Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } // "Credit" or "Debit"
        public string Category { get; set; }
    }