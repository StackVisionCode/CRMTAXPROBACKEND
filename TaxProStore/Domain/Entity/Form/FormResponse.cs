
using Application.Common;
using Application.Domain.Entity.Templates;

namespace Domain.Entity.Form;

public class FormResponse : BaseEntity
{    public Guid FormInstanceId { get; set; }
    public required FormInstance FormInstance { get; set; }
    public required string Data { get; set; } // JSON con todas las respuestas
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

}