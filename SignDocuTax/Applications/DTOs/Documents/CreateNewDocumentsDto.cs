namespace DTOs.Documents
{
    public class CreateNewDocumentsDto
    {
        public required int CompanyId { get; set; }
        public required int TaxUserId { get; set; }
        public required string? Name { get; set; } 
        public required int DocumentStatusId { get; set; }
        public required int DocumentTypeId { get; set; }
        
        
          public required IFormFile? File { get; set; }
    }
}