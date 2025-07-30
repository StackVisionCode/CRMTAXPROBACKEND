using AuthService.Domains.Geography;
using Common;

namespace AuthService.Domains.Addresses;

public class Address : BaseEntity
{
    public int CountryId { get; set; } // 220 = Estados Unidos
    public Country Country { get; set; } = null!;

    public int StateId { get; set; } // 1..51
    public State State { get; set; } = null!;

    // Campos canónicos
    public string? City { get; set; }
    public string? Street { get; set; } // Ej. "123 Main St"
    public string? Line { get; set; } // Ej. "Apt 4B"
    public string? ZipCode { get; set; } // Ej. "33101" o "33101-1234"
}
