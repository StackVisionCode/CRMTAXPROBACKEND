using System.ComponentModel.DataAnnotations;

namespace Application.Common;

public abstract class BaseEntity
{
    [Key]
    public required Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeleteAt { get; set; }
}
