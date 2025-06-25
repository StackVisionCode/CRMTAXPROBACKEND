namespace CommLinkServices.Application.DTOs;

public class CallEndedDto
{
    public Guid ConversationId { get; set; }
    public Guid CallId { get; set; }
    public Guid EndedById { get; set; } // nombre explícito
    public int DurationSeconds { get; set; }
    public DateTime EndedAt { get; set; }
}
