using Domain.Documents;

namespace DTOs.Documents
{
    public class ReadDocumentsDto
    {
  public int Id { get; set; }
        public int CompanyId { get; set; }
        public int TaxUserId { get; set; }
        public string? Name { get; set; }
        public string? FileName { get; set; }
        public int DocumentStatusId { get; set; }
       public string? DocumentStatusName { get; set; } // solo el nombre
        public int DocumentTypeId { get; set; }
        public string? DocumentTypeName { get; set; } // solo el nombre
        public string? Path { get; set; }
    }
}