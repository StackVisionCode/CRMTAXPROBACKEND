namespace Application.DTOS;

   public class StatementDto
    {
        public Guid? Id { get; set; }
        public string AccountNumber { get; set; }
        public string StatementDate { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<TransactionDto> Transactions { get; set; }
        public string UploadedAt { get; set; }
    }