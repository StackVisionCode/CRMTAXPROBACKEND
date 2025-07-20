using Application.Helpers;

namespace Domain.Entities;

public class Signer : BaseEntity
{
    public Guid? CustomerId { get; private set; }
    public string Email { get; private set; } = default!;
    public int Order { get; private set; }
    public SignerStatus Status { get; private set; }
    public string? FullName { get; private set; }

    // Resultado de la firma
    public string? SignatureImage { get; private set; } // base64
    public DigitalCertificate? Certificate { get; private set; }
    public DateTime? SignedAtUtc { get; private set; }
    public string? ClientIp { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime? ConsentAgreedAtUtc { get; private set; }
    public string? ConsentText { get; private set; }
    public bool? ConsentButtonText { get; private set; }

    public Guid SignatureRequestId { get; private set; }
    public string Token { get; private set; } = default!;
    public DateTime? RejectedAtUtc { get; private set; }
    public string? RejectReason { get; private set; }

    // --------------  cajas de firma (1-N) --------------
    private readonly List<SignatureBox> _boxes = new();
    public IReadOnlyCollection<SignatureBox> Boxes => _boxes.AsReadOnly();

    private Signer() { } // ←  requerido por EF

    public Signer(
        Guid signerId,
        Guid? customerId,
        string email,
        int order,
        Guid requestId,
        string token,
        string? fullName = null
    )
    {
        Id = signerId;
        CustomerId = customerId;
        Email = email;
        Order = order;
        SignatureRequestId = requestId;
        Token = token;
        Status = SignerStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        FullName = NormalizeFullName(fullName);
    }

    public void UpdateFullName(string? fullName)
    {
        var normalized = NormalizeFullName(fullName);
        if (string.Equals(FullName, normalized, StringComparison.Ordinal))
            return;
        FullName = normalized;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string? NormalizeFullName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var cleaned = System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"\s+", " ");
        // Capitalización simple (puedes hacerla más compleja)
        return string.Join(
            " ",
            cleaned
                .Split(' ')
                .Select(p =>
                    p.Length == 0
                        ? p
                        : char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant()
                )
        );
    }

    // Método actualizado para crear cajas como entidades independientes
    public void AddBoxes(IEnumerable<SignatureBox> boxes)
    {
        foreach (var box in boxes)
        {
            // Verificar que la caja pertenece a este signer
            if (box.SignerId != Id)
                throw new InvalidOperationException(
                    $"SignatureBox {box.Id} no pertenece al Signer {Id}"
                );

            _boxes.Add(box);
        }
    }

    // Método auxiliar para crear una nueva caja
    public SignatureBox CreateBox(
        int pageNumber,
        float posX,
        float posY,
        float width,
        float height,
        BoxKind kind,
        IntialEntity? initialEntity = null,
        FechaSigner? fechaSigner = null
    )
    {
        var box = new SignatureBox(
            Id,
            pageNumber,
            posX,
            posY,
            width,
            height,
            kind,
            initialEntity,
            fechaSigner
        );
        _boxes.Add(box);
        return box;
    }

    public void RegisterConsent(
        DateTime agreedAtUtc,
        string? consentText,
        bool? consentButton,
        string clientIp,
        string userAgent
    )
    {
        if (Status == SignerStatus.Signed)
            return; // ya firmó, no forzamos excepción; lo hacemos idempotente

        // Si ya existe consentimiento previo podemos decidir:
        // - retornar (idempotencia)
        // - o actualizar (último gana)
        // Aquí lo dejamos idempotente (no sobrescribe):
        if (ConsentAgreedAtUtc.HasValue)
            return;

        ConsentAgreedAtUtc = agreedAtUtc;
        ConsentText = consentText;
        ConsentButtonText = consentButton;
        ClientIp = clientIp;
        UserAgent = userAgent;
        UpdatedAt = DateTime.UtcNow;
    }

    // ---------------- resultado de la firma ----------------
    internal void MarkSigned(
        string imageB64,
        DigitalCertificate cert,
        DateTime signedUtc,
        string ip,
        string ua,
        DateTime consentUtc,
        string? consentText,
        bool? consentButton
    )
    {
        SignatureImage = imageB64;
        Certificate = cert;
        SignedAtUtc = signedUtc;
        ClientIp = ip;
        UserAgent = ua;
        ConsentAgreedAtUtc = consentUtc;
        ConsentText = consentText;
        ConsentButtonText = consentButton;
        Status = SignerStatus.Signed;
        UpdatedAt = DateTime.UtcNow;

        // ─── Marcar cada SignatureBox ──────────────────────
        foreach (var box in _boxes)
            box.UpdatedAt = DateTime.UtcNow; // 1 sola línea
    }

    internal void MarkRejected(string? reason)
    {
        if (Status == SignerStatus.Signed)
            throw new InvalidOperationException("No se puede rechazar un firmante ya firmado.");

        if (Status == SignerStatus.Rejected)
            return; // idempotente

        Status = SignerStatus.Rejected;
        RejectReason = reason;
        RejectedAtUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
