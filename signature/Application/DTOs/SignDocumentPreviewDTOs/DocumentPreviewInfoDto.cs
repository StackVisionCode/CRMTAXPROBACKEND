namespace Application.DTOs;

public class DocumentPreviewInfoDto
{
    public Guid SealedDocumentId { get; set; }
    public Guid OriginalDocumentId { get; set; }
    public Guid SignatureRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalPages { get; set; }
    public string SealedAtUtc { get; set; } = string.Empty;
    public List<SignatureLocationDto> SignatureLocations { get; set; } = new();
    public List<SignerPreviewDto> Signers { get; set; } = new();
    public DocumentPreviewAccessDto PreviewAccess { get; set; } = new();
    public DocumentPreviewStatusDto Status { get; set; } = new();
}
