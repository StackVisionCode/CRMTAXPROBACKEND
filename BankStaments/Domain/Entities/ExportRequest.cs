namespace Domain.Entities;
 public class ExportRequest
    {
        public Guid StatementId { get; set; }
        public string Format { get; set; } // "PDF", "Excel", "CSV"
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] TransactionTypes { get; set; }
        public string[] Categories { get; set; }
    }