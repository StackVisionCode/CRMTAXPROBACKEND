namespace Application.DTOS;

public class TaxDto
{
    public string Name { get; set; } = null!;

    public string Abbreviation { get; set; } = null!;

    public decimal Rate { get; set; }

    public string? Description { get; set; }

    public string? TaxNumber { get; set; }

    public bool ShowTaxNumberOnInvoice { get; set; }

    public bool IsRecoverable { get; set; }

    public bool IsCompound { get; set; }
}