namespace CommLinkServices.Application.DTOs;

public class SendMessageRequestDto
{
    public string Content { get; set; } = string.Empty;
    public bool HasAttachment { get; set; }
    public string? AttachmentUrl { get; set; }
}
