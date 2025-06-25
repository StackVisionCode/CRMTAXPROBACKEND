namespace CommLinkServices.Application.DTOs;

public class ConversationDto
{
    public Guid Id { get; set; }
    public Guid OtherUserId { get; set; }
    public DateTime LastActivity { get; set; }
    public string? LastSnippet { get; set; }
}
