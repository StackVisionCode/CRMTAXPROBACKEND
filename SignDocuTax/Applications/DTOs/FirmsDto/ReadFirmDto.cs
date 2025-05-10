using Domains.Firms;
using Domains.Signatures;
using Dtos.FirmsStatusDto;
using Dtos.SignatureTypeDto;

namespace DTOs.FirmsDto;
public class ReadFirmDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int SignatureTypeId { get; set; }
    public int CompanyId { get; set; }
    public int TaxUserId { get; set; }
    public int FirmStatusId { get; set; }
    public string? Name { get; set; }

    public FirmStatusDto? FirmStatus { get; set; }
    public SignatureTypeDto? SignatureType { get; set; }
    public string Path { get; set; } = string.Empty;
}