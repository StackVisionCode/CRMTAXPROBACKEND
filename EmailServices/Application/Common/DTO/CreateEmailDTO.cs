namespace Application.Common.DTO;

using System.ComponentModel.DataAnnotations;

public class CreateEmailDTO
{
    public required Guid ConfigId { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid CreatedByTaxUserId { get; set; }
    public required Guid SentByTaxUserId { get; set; }

    public string? FromAddress { get; set; }

    public required string ToAddresses { get; set; } = string.Empty;

    public string? CcAddresses { get; set; }
    public string? BccAddresses { get; set; }

    public required string Subject { get; set; } = string.Empty;

    public required string Body { get; set; } = string.Empty;
}
