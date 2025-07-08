using Application.Helpers;

namespace Domain.Entities;

public class SignatureBox : BaseEntity
{
    // ①  Propiedades escalares ---------------------------
    public int PageNumber { get; private set; }
    public float PositionX { get; private set; }
    public float PositionY { get; private set; }
    public float Width { get; private set; }
    public float Height { get; private set; }

    // ②  Owned-types anidados -----------------------------
    public IntialEntity? InitialEntity { get; private set; }
    public FechaSigner? FechaSigner { get; private set; }

    private SignatureBox() { } // ← EF lo usará

    public SignatureBox(
        int pageNumber,
        float posX,
        float posY,
        float width,
        float height,
        IntialEntity? initialEntity = null,
        FechaSigner? fechaSigner = null
    )
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        PageNumber = pageNumber;
        PositionX = posX;
        PositionY = posY;
        Width = width;
        Height = height;
        InitialEntity = initialEntity;
        FechaSigner = fechaSigner;
    }
}
