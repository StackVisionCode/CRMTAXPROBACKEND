namespace DTOs.FirmsDto;
public class UpdateFirmDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int SignatureTypeId { get; set; }
        public int CompanyId { get; set; }
        public int TaxUserId { get; set; }
        public int FirmStatusId { get; set; }
        public string? Name { get; set; }
        public string Path { get; set; } = string.Empty;
    }