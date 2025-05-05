namespace DTOs.FirmsDto;

 public class CreateFirmDto
    {
        public required int CustomerId { get; set; }
        public required int SignatureTypeId { get; set; }
        public required int CompanyId { get; set; }
        public required int TaxUserId { get; set; }
        public required int FirmStatusId { get; set; }
        public required string? Name { get; set; }
        public required IFormFile? File { get; set; }
    }