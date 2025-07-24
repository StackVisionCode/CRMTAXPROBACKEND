namespace Domain.Entities;


public class Tax
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Abbreviation { get; set; } = null!;
    public decimal Rate { get; set; }  // Solo número, sin símbolo %
    public string? Description { get; set; }
    public string? TaxNumber { get; set; }
    public bool ShowTaxNumberOnInvoice { get; set; }
    public bool IsRecoverable { get; set; }
    public bool IsCompoundd { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
     public DateTime? UpdatedAt { get; set; }
}

    

