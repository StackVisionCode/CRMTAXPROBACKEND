using System;
namespace Application.Dtos;

public class PurchaseDto
{
    public Guid Id { get; set; }
    public Guid BuyerId { get; set; }
    public Guid ProductId { get; set; }
    public string PaymentStatus { get; set; } = "Paid"; // o Pending, Failed
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}