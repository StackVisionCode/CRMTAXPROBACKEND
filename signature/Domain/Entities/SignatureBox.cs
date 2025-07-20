using Application.Helpers;

namespace Domain.Entities;

public class SignatureBox : BaseEntity
{
    // ①  Foreign Key hacia Signer ---------------------------
    public Guid SignerId { get; private set; }

    // ②  Propiedades escalares ---------------------------
    public int PageNumber { get; private set; }
    public float PositionX { get; private set; }
    public float PositionY { get; private set; }
    public float Width { get; private set; }
    public float Height { get; private set; }
    public BoxKind Kind { get; private set; }

    // ③  Owned-types anidados -----------------------------
    public IntialEntity? InitialEntity { get; private set; }
    public FechaSigner? FechaSigner { get; private set; }

    // ④  Navegación hacia Signer -------------------------
    public Signer Signer { get; private set; } = default!;

    private SignatureBox() { } // ← EF lo usará

    public SignatureBox(
        Guid signerId,
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
        Id = Guid.NewGuid();
        SignerId = signerId;
        CreatedAt = DateTime.UtcNow;
        PageNumber = pageNumber;
        PositionX = posX;
        PositionY = posY;
        Width = width;
        Height = height;
        Kind = kind;
        InitialEntity = initialEntity;
        FechaSigner = fechaSigner;
    }

    // Método auxiliar para determinar el tipo automáticamente
    public static BoxKind DetermineKind(IntialEntity? initialEntity, FechaSigner? fechaSigner)
    {
        if (initialEntity != null)
            return BoxKind.Initials;
        if (fechaSigner != null)
            return BoxKind.Date;
        return BoxKind.Signature;
    }
}
