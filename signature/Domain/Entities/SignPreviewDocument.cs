using System.ComponentModel.DataAnnotations;
using Application.Helpers;

namespace Domain.Entities;

public class SignPreviewDocument : BaseEntity
{
    [Required]
    public Guid SignatureRequestId { get; private set; }

    [Required]
    public Guid SignerId { get; private set; }

    [Required]
    public Guid OriginalDocumentId { get; private set; }

    /// <summary>
    /// ID del documento sellado en el proyecto externo
    /// </summary>
    [Required]
    public Guid SealedDocumentId { get; private set; }

    /// <summary>
    /// Token de acceso único para el preview
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string AccessToken { get; private set; } = default!;

    /// <summary>
    /// ID de sesión único para el preview
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string SessionId { get; private set; } = default!;

    /// <summary>
    /// Huella digital de la solicitud para validación adicional
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RequestFingerprint { get; private set; } = default!;

    /// <summary>
    /// Fecha y hora de expiración del acceso
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Indica si el acceso está activo
    /// </summary>
    [Required]
    public bool IsActive { get; private set; }

    /// <summary>
    /// Número de veces que se ha accedido al preview
    /// </summary>
    [Required]
    public int AccessCount { get; private set; }

    /// <summary>
    /// Número máximo de accesos permitidos
    /// </summary>
    [Required]
    public int MaxAccessCount { get; private set; }

    /// <summary>
    /// Fecha del último acceso
    /// </summary>
    public DateTime? LastAccessedAt { get; private set; }

    /// <summary>
    /// IP desde la cual se accedió por última vez
    /// </summary>
    [MaxLength(45)] // IPv6
    public string? LastAccessIp { get; private set; }

    /// <summary>
    /// User Agent del último acceso
    /// </summary>
    [MaxLength(500)]
    public string? LastAccessUserAgent { get; private set; }

    // Navegación (sin [ForeignKey] - se configura en OnModelCreating)
    public virtual SignatureRequest SignatureRequest { get; set; } = default!;
    public virtual Signer Signer { get; set; } = default!;

    // Constructor privado para EF
    private SignPreviewDocument() { }

    // Constructor público
    public SignPreviewDocument(
        Guid signatureRequestId,
        Guid signerId,
        Guid originalDocumentId,
        Guid sealedDocumentId,
        string accessToken,
        string sessionId,
        string requestFingerprint,
        DateTime expiresAt,
        int maxAccessCount = 3
    )
    {
        Id = Guid.NewGuid();
        SignatureRequestId = signatureRequestId;
        SignerId = signerId;
        OriginalDocumentId = originalDocumentId;
        SealedDocumentId = sealedDocumentId;
        AccessToken = accessToken;
        SessionId = sessionId;
        RequestFingerprint = requestFingerprint;
        ExpiresAt = expiresAt;
        MaxAccessCount = maxAccessCount;
        IsActive = true;
        AccessCount = 0;
        CreatedAt = DateTime.UtcNow;
    }

    // Métodos para actualizar estado
    public void UpdateAccessCredentials(
        string accessToken,
        string sessionId,
        string requestFingerprint,
        DateTime expiresAt
    )
    {
        AccessToken = accessToken;
        SessionId = sessionId;
        RequestFingerprint = requestFingerprint;
        ExpiresAt = expiresAt;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordAccess(string? clientIp, string? userAgent)
    {
        var previousCount = AccessCount;

        AccessCount++;
        LastAccessedAt = DateTime.UtcNow;
        LastAccessIp = clientIp;
        LastAccessUserAgent = userAgent;
        UpdatedAt = DateTime.UtcNow;

        if (AccessCount >= MaxAccessCount)
        {
            IsActive = false;
        }
    }

    public bool IsRecentAccess(int thresholdSeconds = 30)
    {
        if (!LastAccessedAt.HasValue)
            return false;

        var timeSinceLastAccess = DateTime.UtcNow - LastAccessedAt.Value;
        return timeSinceLastAccess.TotalSeconds < thresholdSeconds;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanAccess()
    {
        return IsActive && ExpiresAt > DateTime.UtcNow && AccessCount < MaxAccessCount;
    }
}
