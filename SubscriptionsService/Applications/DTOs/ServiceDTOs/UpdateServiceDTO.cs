using System.ComponentModel.DataAnnotations;

namespace DTOs.ServiceDTOs;

public class UpdateServiceDTO
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required List<string> Features { get; set; }

    [Range(0, double.MaxValue)]
    public required decimal Price { get; set; }

    [Range(1, int.MaxValue)]
    public required int UserLimit { get; set; }
    public bool IsActive { get; set; } = true;
}
