namespace CommLinkServices.Application.DTOs;

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool HasAttachment { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime SentAt { get; set; }
}
