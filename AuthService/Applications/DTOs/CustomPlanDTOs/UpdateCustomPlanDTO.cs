using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.CustomPlanDTOs;

public class UpdateCustomPlanDTO
{
    public required Guid Id { get; set; }

    [Range(0, double.MaxValue)]
    public required decimal Price { get; set; }

    [Range(1, int.MaxValue)]
    public required int UserLimit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime RenewDate { get; set; }
    public bool isRenewed { get; set; } = false;
    public DateTime? RenewedDate { get; set; }
}
